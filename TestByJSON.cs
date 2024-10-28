using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace JsonUnitSimplifier
{
    public class TestByJSON
    {

        /// <summary>
        /// Автоматическое тестирование, определяются тетсируемые типы и классы, дополнительная
        /// лоигка отсутствует
        /// </summary>
        /// <param name="json">Строка, в которой содержатся данные о юнит-тесте</param>
        /// <exception cref="Exception"></exception>
        public static void AutoTestByJSON(string json)
        {
            var test = JSON_Parser.Parse(json); // Получаем нормальный объект для работы с тестом
            Console.WriteLine("Autotest mode: " + test.id);
            Type type = null;

            if (test.classes.Count == 1)
            {
                type = Type.GetType(test.classes[0]);
                if (type == null)
                {
                    throw new Exception($"Class '{test.classes[0]}' not found.");
                }

                // Вызываем TestObject с пустым делегатом
                var method = typeof(TestByJSON).GetMethod("TestObject").MakeGenericMethod(type);
                method.Invoke(null, new object[] { json, new Action<object>(obj => { }) });

            }
            else if (test.classes.Count == 4)
            {
                string className = test.classes[0];
                string serviceName = test.classes[1]; 
                string mockName = test.classes[2];
                string insertMethodName = test.classes[3]; 

                // Получаем типы по именам классов
                Type classType = Type.GetType(className);
                Type serviceType = Type.GetType(serviceName);
                Type mockType = Type.GetType(mockName);

                if (classType == null || serviceType == null || mockType == null)
                {
                    throw new Exception($"One or more classes not found. Only: {classType} {serviceType} {mockType} have been found");
                }

                // Создаем экземпляр сервиса и мока
                var mock = Activator.CreateInstance(mockType);
                var serviceInstance = Activator.CreateInstance(serviceType, mock);

                // Создаем делегаты
                var insertMethod = serviceType.GetMethod(insertMethodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                var insertDelegate = Delegate.CreateDelegate(typeof(Action<,>).MakeGenericType(serviceType, classType), insertMethod);
                var testLogicDelegate = (Action<object, object[]>)((a, b) =>{});

                if (mock == null) throw new Exception("Can't create a mock (argument for conssctructor) => Service instance is null");
                if (serviceInstance == null) throw new Exception("Can't create Service instance");
                if (insertDelegate == null) throw new Exception("Can't create insert delegate");

                // Теперь вызываем метод TestServiceMVVM с нужными типами
                var testLayered = typeof(TestByJSON).GetMethod("TestLayeredService").MakeGenericMethod(classType, serviceType);
                testLayered.Invoke(null, new object[] { json, serviceInstance, insertDelegate, testLogicDelegate });              
            }
            else
            {
                throw new Exception("Invalid class array. There must be 1 (for testing dataset of this class) or 3 classes (MVVM testing) + 1 method name: [target class, service class, mock class, service class insert function]");
            }
        }


        /// <summary>
        /// Полностью автоматическое тестирование без доп. логики
        /// Автоматически определяются тетсируемые типы и классы
        /// </summary>
        /// <param name="jsons">JSON строки для работы с юнит-тестами</param>
        public static void AutoTestByJSONs(
            string jsons_path
            )
        {
            var files = Directory.GetFiles(jsons_path, "*.json");
            string fail_message = "";
            int failed_count = 0;

            foreach (var json in files)
            {
                try
                {
                    AutoTestByJSON(json);
                }
                catch (Exception ex)
                {
                    failed_count++;
                    fail_message += $"\nFAIL: {json} CAUSE \n{ex.InnerException?.GetType().Name}\n{ex.InnerException?.Message}\n{ex.StackTrace}\n";                
                }
            }

            if (failed_count > 0)
            {
                throw new Exception($"{failed_count}/{files.Length} tests FAILED. Errors: \n{fail_message}");
            }
            else
            {
                Console.WriteLine($"All the tests {files.Length}/{files.Length} are successful");
            }
        }


        /// <summary>
        /// Для тестирования паттерна слоистой архитектуры. Где служба управляет объектами модели БД 
        /// (есть возможность создать заглушку) и должна быть
        /// функция добавления объектов, функцию добавления пробросить в лямбде.
        /// Тестируются как сами объекты датасета, так и сама служба.
        /// Цели проверок (assert) указываются в JSON
        /// </summary>
        /// <typeparam name="Class"></typeparam>
        /// <typeparam name="Service"></typeparam>
        /// <param name="json">Строка, описывающая юнит-тест</param>
        /// <param name="service">Служба, управляющая бизнес-логикой и объектами датасета</param>
        /// <param name="insert">Переброска функции добавления в службу тестовых данных</param>
        /// <param name="testLogic">Дополнительная логика тестирования, которую нельзя описать в JSON файле</param>
        /// <exception cref="Exception"></exception>
        public static void TestLayeredService<Class, Service>(
            string json,
            Service service,
            Action<Service, Class> insert,
            Action<Service, Class[]> testLogic
            )
        {
            var test = JSON_Parser.Parse(json); // Получаем нормальный объект для работы с тестом
            Console.WriteLine(test.id + " as TestLayeredService started");
            Class[] dataset = getDataset<Class>(test); // И сам датасет
            foreach (var item in dataset)
            {
                insert(service, item);
            }

            // Ассерты до доп. логики
            if (test.assert_before_lambda != null)
            {
                foreach (var a in test.assert_before_lambda)
                {
                    if (a.target == "service-to-object")
                    {
                        int i = 0;
                        foreach (var item in dataset)
                        {
                            try
                            {
                                Assert<Class, Service>(item, service, a, i);
                                i++;
                            }
                            catch (Exception ex)
                            {
                                throw new Exception($"Assert of {a.function}{a.method}(dataset[{i}], ...) failed: {ex.Message}");
                            }

                        }
                    }
                    else if (a.target == "service")
                    {
                        try
                        {
                            if (a.args[0] is JArray)
                            {
                                for (int i = 0; i < a.args.Count; i++)
                                {
                                    Assert<Service>(service, a, i);
                                }
                            }
                            else
                                Assert<Service>(service, a, 0);
                        }
                        catch (Exception ex)
                        {
                            throw new Exception($"Assert of {a.type_assert} {a.function}{a.method} service class failed: {ex.Message}");
                        }
                    }
                    else if (a.target == "objects")
                    {
                        int i = 0;
                        foreach (var item in dataset)
                        {
                            try
                            {
                                Assert(item, a, i);
                                i++;
                            }
                            catch (Exception ex)
                            {
                                throw new Exception($"Assert of {a.type_assert} {a.function}{a.method} dataset[{i}] failed: {ex.Message}");
                            }
                        }
                    }
                    else { throw new Exception($"{a.target} is not a valid target. It can be only service_to_object, service or objects"); }
                }
            }

            if (testLogic != null)
            {
                testLogic(service, dataset);
            }

            // Ассерты после доп. логики
            if (test.assert_after_lambda != null)
            {
                foreach (var a in test.assert_after_lambda)
                {
                    if (a.target == "service-to-object")
                    {
                        int i = 0;
                        foreach (var item in dataset)
                        {
                            try
                            {
                                Assert<Class, Service>(item, service, a, i);
                                i++;
                            }
                            catch (Exception ex)
                            {
                                throw new Exception($"Assert of {a.function}{a.method}(dataset[{i}], ...) failed: {ex.Message}");
                            }

                        }
                    }
                    else if (a.target == "service")
                    {
                        try
                        {
                            if (a.args[0] is JArray)
                            {
                                for (int i = 0; i < a.args.Count; i++)
                                {
                                    Assert<Service>(service, a, i);
                                }
                            }
                            else
                                Assert<Service>(service, a, 0);
                        }
                        catch (Exception ex)
                        {
                            throw new Exception($"Assert of {a.type_assert} {a.function}{a.method} service class failed: {ex.Message}");
                        }
                    }
                    else if (a.target == "objects")
                    {
                        int i = 0;
                        foreach (var item in dataset)
                        {
                            try
                            {
                                Assert(item, a, i);
                                i++;
                            }
                            catch (Exception ex)
                            {
                                throw new Exception($"Assert of {a.type_assert} {a.function}{a.method} dataset[{i}] failed: {ex.Message}");
                            }
                        }
                    }
                    else { throw new Exception($"{a.target} is not a valid target. It can be only service-to-object, service or objects"); }
                }
            }

        }

        /// <summary>
        /// Тестированире конкретного класса, то есть набора объектов этого класса 
        /// </summary>
        /// <param name="json">Строка JSON, которая соответствует нужному юнит-тесту</param>
        /// <param name="testLogic">Доп. логика для тестирования, которую не удалось описать в JSON</param>
        public static void TestObject<Class>(
                string json,
                Action<Class> testLogic
            )
        {
            var test = JSON_Parser.Parse(json); // Получаем нормальный объект для работы с тестом
            Class[] dataset = getDataset<Class>(test); // И сам датасет
            Console.WriteLine(test.id + " start as a usual test");

            // Ассерты до доп. логики
            if (test.assert_before_lambda != null)
            {
                int i = 0;
                foreach (var item in dataset)
                {
                    foreach (var a in test.assert_before_lambda)
                    {
                        try
                        {
                            Assert(item, a, i);
                        }
                        catch (Exception ex)
                        {
                            throw new Exception($"Assert of {a.type_assert} {a.function}{a.method}{a.field} dataset[{i}] failed: {ex.Message}\n{ex.StackTrace}\n{ex.InnerException?.StackTrace}");
                        }
                    }
                    i++;
                }
            }

            // Доп. логика
            foreach (var item in dataset)
            {
                testLogic(item);
            }

            // Ассерты после доп. логики
            if (test.assert_after_lambda != null)
            {
                int i = 0;
                foreach (var item in dataset)
                {
                    foreach (var a in test.assert_after_lambda)
                    {
                        try
                        {
                            Assert(item, a, i);
                        }
                        catch (Exception ex)
                        {
                            throw new Exception($"Assert of {a.type_assert} {a.function}{a.method}{a.field} dataset[{i}] failed: {ex.Message}");
                        }
                    }
                    i++;
                }
            }
        }


        /// <summary>
        /// Генерация датасета согласно указанным в JSON параметрам
        /// </summary>
        /// <typeparam name="T">Тип данных датасета</typeparam>
        /// <param name="test">Класс юннит-теста (получаемый после парсинга файла)</param>
        /// <exception cref="InvalidCastException"></exception>
        private static T[] getDataset<T>(UnitTest test)
        {
            if (test.rules.Count == 0)
                return new T[0];

            // Правила генерации для разных режимов
            List<Func<int, object>> generation_rules_l = new List<Func<int, object>>(); 
            // Если создавать конструктором
            
            Dictionary<string, Func<int, object>> generation_rules_d = new Dictionary<string, Func<int, object>>();
            // Если задавать поля

            // Определение правил (парсинг из JSON'а)
            if (test.mode == "fields") //Объекты T создавать не конструктором, а задавая поля
            {
                foreach (var rule in test.rules)
                {
                    generation_rules_d.Add(rule.field, get_generation_rule(rule, typeof(T)));
                }
            }
            else if (test.mode == "constructor") //Объекты создавать конструктором, который соответствует функциям
            {
                foreach (var rule in test.rules)
                {
                    generation_rules_l.Add(get_generation_rule(rule, typeof(T)));
                }
            }
            else { throw new InvalidCastException($"Unknown mode in JSON file (\"mode\" must be \"fields\" or \"constructor)\""); }


            // Все возможные комбинации
            if (test.combination_mode == "all-to-all")
            {
                int combinationsCount = 1; // Счёт числа комбинаций
                foreach (var rule in test.rules)
                {
                    combinationsCount *= rule.Combinations;
                }

                T[] dataset = new T[combinationsCount];

                // Конструктором перебираем все возможные варианты
                if (test.mode == "constructor")
                {
                    // Массив, где строка - объект, столбец - аргумент конструктора
                    var combinations = new object[combinationsCount, test.rules.Count];

                    // Даже не пытайся понять этот алгоритм, но он работает, заполняя массив
                    int steps_before_incrimination = combinationsCount;
                    for (int i = 0; i < test.rules.Count; i++)
                    {
                        steps_before_incrimination /= test.rules[i].Combinations;
                        int current_arg = -1;
                        int steps = 0;
                        for (int j = 0; j < combinationsCount; j++, steps++)
                        {
                            if (steps % steps_before_incrimination == 0)
                                current_arg++;

                            combinations[j, i] = generation_rules_l[i](current_arg);
                        }
                    }

                    // По массиву объекты * аргументы создаём сам датасет
                    for (int i = 0; i < combinationsCount; i++)
                    {
                        var parameters = new object[test.rules.Count];
                        for (int j = 0; j < test.rules.Count; j++)
                        {
                            parameters[j] = combinations[i,j];
                        }
                        dataset[i] = (T)Activator.CreateInstance(typeof(T), parameters);
                    }
                }
                else if (test.mode == "fields") // Задаём поля, нужен пустой конструктор
                {
                    // Массив объекты: (поле: значение)
                    var combinations = new Dictionary<string, object>[combinationsCount];

                    // Этот алгоритм работает, всё, что нужно знать, еле-как вывел
                    int steps_before_incrimination = combinationsCount;
                    for (int i = 0; i < test.rules.Count; i++)
                    {
                        steps_before_incrimination /= test.rules[i].Combinations;
                        int current_arg = -1;
                        int steps = 0;

                        for (int j = 0; j < combinationsCount; j++, steps++)
                        {
                            if (steps % steps_before_incrimination == 0)
                                current_arg++;

                            if (combinations[j] == null)
                                combinations[j] = new Dictionary<string, object>();

                            combinations[j].Add(test.rules[i].field, generation_rules_d[test.rules[i].field](current_arg));
                        }
                    }

                    // По созданным комбинациям создаём объекты, задаём поля, короче, делаем датасет
                    for (int i = 0; i < combinationsCount; i++)
                    {
                        var instance = Activator.CreateInstance(typeof(T));
                        foreach (var key in combinations[i].Keys)
                        {
                            var fieldValue = combinations[i][key];
                           

                            var propertyInfo = typeof(T).GetProperty(key, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                            if (propertyInfo != null)
                                propertyInfo.SetValue(instance, fieldValue);
                            else
                            {
                                var fieldInfo = typeof(T).GetField(key, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                                fieldInfo.SetValue(instance, fieldValue);
                            }
                        }
                        dataset[i] = (T)instance;
                    }
                }
                else { throw new InvalidCastException(); }

                return dataset;
            }
            else if (test.combination_mode == "simple") // Минимум комбинаций
            {
                int combinationsCount = 1; // Число комбинаций - максимум из возможных значений полей
                foreach (var rule in test.rules)
                {
                    if (combinationsCount < rule.Combinations)
                        combinationsCount = rule.Combinations;
                }

                T[] dataset = new T[combinationsCount];

                if (test.mode == "constructor") // По конструктору просто перебираем, rule(i) всё равно зациклены
                {
                    for (int i = 0; i < combinationsCount; i++)
                    {
                        var parameters = generation_rules_l.Select(rule => rule(i)).ToArray();

                        dataset[i] = (T)Activator.CreateInstance(typeof(T), parameters);
                    }
                }
                else if (test.mode == "fields") // Аналогично с полями
                {
                    for (int i = 0; i < combinationsCount; i++)
                    {
                        var instance = Activator.CreateInstance(typeof(T));
                        foreach (var kvp in generation_rules_d)
                        {
                            var fieldValue = kvp.Value(i);
                            var propertyInfo = typeof(T).GetProperty(kvp.Key, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                            
                            if (propertyInfo != null)
                                propertyInfo.SetValue(instance, fieldValue);
                            else
                            {
                                var fieldInfo = typeof(T).GetField(kvp.Key, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                                fieldInfo.SetValue(instance, fieldValue);
                            }

                        }
                        dataset[i] = (T)instance;
                    }

                }
                else { throw new InvalidCastException(); }

                return dataset;
            }
            else { throw new InvalidCastException($"Unknown combination_mode in JSON file (it must be \"all-to-all\" or \"simple)\""); }
        }


        /// <summary>
        /// Преобразует указанное правило генерации датасета в ЗАЦИКЛЕННУЮ НА СЕБЯ функцию.
        /// Если правило имеет функциональный тип изначально, зацикливания не происходит
        /// </summary>
        /// <param name="rule">Правило (из массива Rules класса UnitTest, получаемого после парсинга JSON)</param>
        /// <exception cref="Exception"></exception>
        private static Func<int, object> get_generation_rule(Rule rule, Type datasetType)
        {
            var typeOfRule = GetTypeOfRule(rule, datasetType);
            var innerFunc = get_generation_rule_object(rule, datasetType);

            if (typeOfRule != null)
                return i => Convert.ChangeType(innerFunc(i), typeOfRule);
            else return innerFunc;
        }
        private static Func<int, object> get_generation_rule_object(Rule rule, Type datasetType)
        {
            if (rule.values != null) // Список возможных значений
                return i => rule.values[i % rule.values.Count];

            if (rule.value != null) // Одно значение
                return i => rule.value;
            
            if (rule.range != null) // Область значений
            {
                int steps = (int)((rule.range[1] - rule.range[0]) / rule.step + 1);

                Type type = GetTypeOfRule(rule, datasetType);
                var values = new object[steps];

                for (int j = 0; j < steps; j++)
                {
                    values[j] = rule.range[0] + rule.step * j;
                }

                return i => values[i % values.Length];
            }
            if (rule.function != null) // Предопределённая функция
            {
                return GenerateFunctions.Get(rule.function);
            }
            else if (rule.random != null)
            {
                if (rule.random is string)
                {
                    var randomStringGenerator = new RandomStringGenerator();
                    return i => randomStringGenerator.Generate(rule.random as string);
                }
                else
                {
                    var rand = rule.random as JArray;                   
                    Random random = new Random();
                    return i => (double)rand[0] + random.NextDouble() * ((double)rand[1] - (double)rand[0]);
                }
            }
            
            throw new Exception($"Generation rule of field '{rule.field}' is incorrect");            
        }
        private static Type GetTypeOfRule(Rule rule, Type DatasetType)
        {
            Type type = null;

            if (rule.field_type != null) // User sets the type
                type = Type.GetType(rule.field_type);
            else if (rule.field != null) // Auto-finding type
            {
                var field = DatasetType.GetField(rule.field);
                var prop = DatasetType.GetProperty(rule.field);

                if (field != null)      return field.FieldType;
                else if (prop != null)  return prop.PropertyType;
            }        
            return type;
        }


        /// <summary>
        /// Выполнение ассерта (проверки), описанного в юнит-тесте (см. файл JSON)
        /// Если не прошёл проверку - исключение
        /// </summary>
        /// <param name="obj">Какой объект проверяем</param>
        /// <param name="assert">Проверка из JSON файла</param>
        /// <param name="i">Номер объекта в датасете</param>
        /// <exception cref="Exception"></exception>
        private static void Assert<T>(T obj, Assert assert, int i)
        {
            // Вызов метода по имени
            if (assert.method != null)
            {
                AssertInvoke<T, T>(obj, assert.method, assert, false, i);
            }
            // Вызов функции по имени
            else if (assert.function != null)
            {
                AssertInvoke<T, T>(obj, assert.function, assert, true, i);
            }
            // Проверка значений полей на основе type_assert
            else if (assert.value != null)
            {
                object actualValue = GetValueFromObject(obj, assert.field);
                AssertByValue(assert.value, actualValue, assert.type_assert, i);
            }
            // Проверка списка результатов
            else if (assert.values != null)
            {
                object actualValue = GetValueFromObject(obj, assert.field);
                AssertByValue(assert.values[i], actualValue, assert.type_assert, i);
            }

            else throw new Exception("Unknown assert type error");
        }


        /// <summary>
        /// Выполнение ассерта (проверки), описанного в юнит-тесте (см. файл JSON)
        /// Если не прошёл проверку - исключение.
        /// Проверка только вызовов функций и методов, где первый аргумент - объект модели.
        /// См. паттерн MVVM
        /// </summary>
        /// <param name="obj">На каком объекте проверяем</param>
        /// <param name="service">Какую службу проверяем</param>
        /// <param name="assert">Проверка из JSON файла</param>
        /// <param name="i">Номер объекта в датасете</param>
        /// <exception cref="Exception"></exception>
        private static void Assert<Class, Service>(Class obj, Service service, Assert assert, int i)
        {
            // Вызов метода по имени
            if (assert.method != null)
            {
                AssertInvoke<Class, Service>(service, assert.method, assert, false, i, obj);
            }
            else if (assert.function != null)
            {
                AssertInvoke<Class, Service>(service, assert.function, assert, true, i, obj);
            }
            else
            {
                throw new Exception("There must be function or method!");
            }
        }


        /// <summary>
        /// Проверка вызова функции, ожидаемого результата. Если isFunction false, тогда вызов сетода
        /// </summary>
        private static void AssertInvoke<DatasetType, InvokeOn>(InvokeOn target, string fname, Assert assert, bool isFunction, int i, object first_arg = null)
        {
            var methodInfo = typeof(InvokeOn).GetMethod(fname);
            if (methodInfo == null)
            {
                throw new Exception($"{assert.method}{assert.function} not found in type {typeof(DatasetType).Name}");
            }

            try
            {
                object[] parameters = new object[0];
                GetParameters(assert, methodInfo, out parameters, i, first_arg);

                var res = methodInfo.Invoke(target, parameters);

                // Проверка исключения
                if (assert.exception != null || (assert.exceptions != null && assert.exceptions[i] != null))
                {
                    throw new Exception($"Expected exception '{assert.exception}{assert.exceptions?[i]}' was not thrown.");
                }

                if (isFunction)
                {
                    object expected = null;
                    if (assert.results != null && assert.results.Count > 0)
                    {
                        expected = assert.results[i];
                    }
                    else
                    {
                        expected = assert.result;
                    }
                    AssertByValue(expected, res, assert.type_assert, i);
                }
            }
            catch (TargetInvocationException ex)
            {
                string expectedException = null;

                if (assert.exceptions != null)
                    expectedException = assert.exceptions[i];
                else
                    expectedException = assert.exception;

                // Проверка на соответствие имени исключения
                if (expectedException != ex.InnerException?.GetType().Name)
                {
                    throw new Exception($"Expected exception '{expectedException}', i = {i}, but got '{ex.InnerException?.GetType()}'\n{ex.InnerException.StackTrace}");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error in {assert.method}{assert.function}\n{ex.GetType().Name}\n{ex.Message}\n" +
                    $"{ex.StackTrace}\n\n{ex.InnerException?.GetType().Name}{ex.InnerException?.Message}\n{ex.InnerException?.StackTrace}");
            }
        }

        /// <summary>
        ///  Вспомогательный метод для получения значения поля у объекта (написан ИИ)
        /// </summary>
        private static object GetValueFromObject<T>(T obj, string fieldName)
        {
            if (string.IsNullOrEmpty(fieldName)) return null;

            var propertyInfo = typeof(T).GetProperty(fieldName);
            if (propertyInfo != null)
            {
                return propertyInfo.GetValue(obj);
            }

            var fieldInfo = typeof(T).GetField(fieldName);
            if (fieldInfo != null)
            {
                return fieldInfo.GetValue(obj);
            }

            throw new Exception($"Field or property '{fieldName}' not found in type {typeof(T).Name}");
        }


        private static void AssertByValue(object expected, object actualValue, string type_assert, int i)
        {
            if (actualValue != null && expected != null)
                actualValue = Convert.ChangeType(actualValue, expected.GetType());

            switch (type_assert)
            {
                case "equals":
                    if (!object.Equals(actualValue, expected))
                        throw new Exception($"Expected value '{expected}', but got '{actualValue}'.");
                    break;

                case "unequals":
                    if (object.Equals(actualValue, expected))
                        throw new Exception($"Expected value to be different from '{expected}', but got the same.");
                    break;

                case "more":
                    if (Convert.ToDouble(actualValue) <= Convert.ToDouble(expected))
                        throw new Exception($"Expected value to be more than '{expected}', but got '{actualValue}'.");
                    break;

                case "lesser":
                    if (Convert.ToDouble(actualValue) >= Convert.ToDouble(expected))
                        throw new Exception($"Expected value to be lesser than '{expected}', but got '{actualValue}'.");
                    break;

                case "regex":
                    var r = new Regex(expected.ToString());
                    if (!r.IsMatch(actualValue.ToString()))
                        throw new Exception($"Expected value to be like regex '{expected}', but got '{actualValue}'.");

                    break;

                case "function":
                    if (actualValue == GenerateFunctions.Get(expected.ToString())(i))
                        throw new Exception($"Expected value to be like regex '{expected}', but got '{actualValue}'.");

                    break;

                default:
                    throw new Exception($"Unknown assertion type: {type_assert}");
            }
        }


        /// <summary>
        /// Получает и генерирует список параметров для вызова функций/методов Assert
        /// </summary>
        private static void GetParameters(Assert assert, MethodInfo methodInfo, out object[] parameters, int i, object firstArg = null)
        {
            var requiredParams = methodInfo.GetParameters();
            List<object> paramList = new List<object>();

            if (firstArg != null)
            {
                paramList.Add(firstArg);
            }

            if (assert.args.Count > 0 && assert.args[0] is JArray)
            {
                var args = assert.args[i] as JArray;

                for (int index = 0; index < args.Count; index++)
                {
                    var arg = args[index];

                    if (arg == null)
                    {
                        paramList.Add(null);
                    }
                    else
                    {
                        paramList.Add(arg.ToObject(requiredParams[index].ParameterType));
                    }
                }
            }
            else
            {
                for (int index = 0; index < assert.args.Count; index++)
                {
                    var arg = assert.args[index];

                    if (arg == null)
                    {
                        paramList.Add(null);
                    }
                    else
                    {
                        paramList.Add(Convert.ChangeType(arg, requiredParams[index].ParameterType));
                    }
                }
            }
            parameters = paramList.ToArray(); // Преобразуем список в массив
        }

    }
}
