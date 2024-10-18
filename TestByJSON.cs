using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
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
                var serviceInstance = Activator.CreateInstance(serviceType, Activator.CreateInstance(mockType));

                // Создаем делегаты
                var insertMethod = serviceType.GetMethod(insertMethodName);
                var insertDelegate = Delegate.CreateDelegate(typeof(Action<,>).MakeGenericType(serviceType, classType), insertMethod);
                var testLogicDelegate = (Action<object, object[]>)((a, b) =>{});

                // Теперь вызываем метод TestServiceMVVM с нужными типами
                var testServiceMVVMMethod = typeof(TestByJSON).GetMethod("TestLayeredService").MakeGenericMethod(classType, serviceType);
                testServiceMVVMMethod.Invoke(null, new object[] { json, serviceInstance, insertDelegate, testLogicDelegate });
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
            string[] jsons
            )
        {
            foreach (var json in jsons)
            {
                AutoTestByJSON(json);
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
                    if (a.target == "service_to_object")
                    {
                        int i = 0;
                        foreach (var item in dataset) {
                            assert<Class, Service>(item, service, a, i);
                        }
                    }
                    else if (a.target == "service")
                    {
                        assert<Service>(service, a, 0);
                    }
                    else if (a.target == "objects")
                    {
                        int i = 0;
                        foreach (var item in dataset)
                        {
                            assert(item, a, i);
                            i++;
                        }
                    }
                    else { throw new Exception($"{a.target} is not a valid target"); }
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
                            assert<Class, Service>(item, service, a, i);
                            i++;
                        }
                    }
                    else if (a.target == "service")
                    {
                        assert<Service>(service, a, 0);
                    }
                    else if (a.target == "objects")
                    {
                        int i = 0;
                        foreach (var item in dataset)
                        {
                            assert(item, a, i);
                            i++;
                        }
                    }
                    else { throw new Exception($"{a.target} is not a valid target"); }
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

            // Ассерты до доп. логики
            if (test.assert_before_lambda != null)
            {
                int i = 0;
                foreach (var item in dataset)
                {
                    foreach (var a in test.assert_before_lambda)
                    {
                        assert(item, a, i);
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
                        assert(item, a, i);
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
                    generation_rules_d.Add(rule.field, get_generation_rule(rule));
                }
            }
            else if (test.mode == "constructor") //Объекты создавать конструктором, который соответствует функциям
            {
                foreach (var rule in test.rules)
                {
                    generation_rules_l.Add(get_generation_rule(rule));
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
                            var fieldInfo = typeof(T).GetProperty(key, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                            fieldInfo.SetValue(instance, fieldValue);
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
                            var fieldInfo = typeof(T).GetProperty(kvp.Key, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                            fieldInfo.SetValue(instance, fieldValue);
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
        /// </summary>
        /// <param name="rule">Правило (из массива Rules класса UnitTest, получаемого после парсинга JSON)</param>
        /// <exception cref="Exception"></exception>
        private static Func<int, object> get_generation_rule(Rule rule)
        {
            if (rule.values != null) // Список возможных значений
            {
                return i => rule.values[i % rule.values.Count];
            }
            else if (rule.value != null) // Одно значение
            {
                return i => rule.value;
            }
            if (rule.range != null) // Область значений
            {
                int steps = (int)((rule.range[1] - rule.range[0]) / rule.step + 1);

                Type type = null;

                if (rule.field_type != null)
                    type = Type.GetType(rule.field_type);

                if (type == null)
                    type = typeof(double);

                var values = new object[steps];

                for (int j = 0; j < steps; j++)
                {
                    values[j] = Convert.ChangeType(rule.range[0] + rule.step * j, type);
                }

                return i => values[i % values.Length];
            }
            else if (rule.function != null) // Предопределённая функция
            {
                return GenerateFunctions.Get(rule.function);
            }
            else
            {
                throw new Exception("Читай документацию, простофиля"); // TODO: прописать нормальные строки исключениям
            }
        }


        /// <summary>
        /// Выполнение ассерта (проверки), описанного в юнит-тесте (см. файл JSON)
        /// Если не прошёл проверку - исключение
        /// </summary>
        /// <param name="obj">Какой объект проверяем</param>
        /// <param name="assert">Проверка из JSON файла</param>
        /// <param name="i">Номер объекта в датасете</param>
        /// <exception cref="Exception"></exception>
        private static void assert<T>(T obj, Assert assert, int i)
        {
            // Вызов метода по имени
            if (assert.method != null)
            {
                var methodInfo = typeof(T).GetMethod(assert.method);
                if (methodInfo == null)
                {
                    throw new Exception($"Method {assert.method} not found in type {typeof(T).Name}");
                }

                try
                {
                    object expected = null;
                    if (assert.result != null)
                    {
                        expected = assert.result;
                    }
                    else
                    {
                        try
                        {
                            expected = assert.results[i];
                        }
                        catch (NullReferenceException)
                        {
                            expected = null;
                        }
                    }

                    Console.WriteLine(i);
                    if (assert.args.Count > 0 && assert.args[0] is JArray)
                    {
                        var args = assert.args[i] as JArray;
                        object[] parameters = args.Select(arg => ((JToken)arg).ToObject(methodInfo.GetParameters()[0].ParameterType)).ToArray();
                        methodInfo.Invoke(obj, parameters);
                    }
                    else
                    {
                        var parameters = assert.args.ConvertAll(arg => Convert.ChangeType(arg, methodInfo.GetParameters()[0].ParameterType)).ToArray();
                        methodInfo.Invoke(obj, parameters);
                    }

                    // Проверка исключения
                    if (assert.exception != null)
                    {
                        throw new Exception($"Expected exception '{assert.exception}' was not thrown.");
                    }
                }
                catch (TargetInvocationException ex)
                {
                    // Проверка на соответствие имени исключения
                    if (assert.exceptions != null && assert.exceptions[i] != null && ex.InnerException?.GetType().Name != assert.exceptions[i])
                    {
                        throw new Exception($"Expected exception '{assert.exceptions[i]}' index of {i}, but got '{ex.InnerException?.GetType().Name}'.");
                    }
                    if (assert.exception != null && ex.InnerException?.GetType().Name != assert.exception)
                    {
                        throw new Exception($"Expected exception '{assert.exception}', but got '{ex.InnerException?.GetType().Name}'.");
                    }
                }
            }

            // Вызов функции по имени
            if (assert.function != null)
            {
                var methodInfo = typeof(T).GetMethod(assert.function);
                if (methodInfo == null)
                {
                    throw new Exception($"Method {assert.function} not found in type {typeof(T).Name}");
                }

                try
                {                   
                    object expected = null;
                    if (assert.result != null)
                    {
                        expected = assert.result;
                    }
                    else
                    {
                        try
                        {
                            expected = assert.results[i];
                        }
                        catch (NullReferenceException)
                        {
                            expected = null;
                        }
                    }

                    Console.WriteLine(i);
                    if  (assert.args.Count > 0 && assert.args[0] is JArray)
                    {
                        var args = assert.args[i] as JArray;
                        object[] parameters = args.Select(arg => ((JToken)arg).ToObject(methodInfo.GetParameters()[0].ParameterType)).ToArray();
                        object got = methodInfo.Invoke(obj, parameters);
                        AssertByValue(expected, got, assert.type_assert, i);
                    }
                    else
                    {
                        var parameters = assert.args.ConvertAll(arg => Convert.ChangeType(arg, methodInfo.GetParameters()[0].ParameterType)).ToArray();
                        object got = methodInfo.Invoke(obj, parameters);
                        AssertByValue(expected, got, assert.type_assert, i);
                    }

                    // Проверка исключения
                    if (assert.exception != null)
                    {
                        throw new Exception($"Expected exception '{assert.exception}' was not thrown.");
                    }
                }
                catch (TargetInvocationException ex)
                {
                    // Проверка на соответствие имени исключения
                    if (assert.exceptions != null && assert.exceptions[i] != null && ex.InnerException?.GetType().Name != assert.exceptions[i])
                    {
                        throw new Exception($"Expected exception '{assert.exceptions[i]}' index of {i}, but got '{ex.InnerException?.GetType().Name}'.");
                    }
                    if (assert.exception != null && ex.InnerException?.GetType().Name != assert.exception)
                    {
                        throw new Exception($"Expected exception '{assert.exception}', but got '{ex.InnerException?.GetType().Name}'.");
                    }
                }
            }

            // Проверка значений полей на основе type_assert
            if (assert.value != null)
            {
                object actualValue = GetValueFromObject(obj, assert.field);
                AssertByValue(assert.value, actualValue, assert.type_assert, i);
            }

            // Проверка списка результатов
            if (assert.values != null)
            {
                object actualValue = GetValueFromObject(obj, assert.field);
                AssertByValue(assert.values[i], actualValue, assert.type_assert, i);
            }       
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
        private static void assert<Class, Service>(Class obj, Service service, Assert assert, int i)
        {
            // Вызов метода по имени
            if (assert.method != null)
            {
                var methodInfo = typeof(Service).GetMethod(assert.method);
                if (methodInfo == null)
                {
                    throw new Exception($"Method {assert.method} not found in type {typeof(Service).Name}");
                }

                try
                {
                    if (assert.args.Count > 0 && assert.args[0] is JArray)
                    {
                        var args = assert.args[i] as JArray;
                        var parameters = args.Select(arg => ((JToken)arg).ToObject(methodInfo.GetParameters()[0].ParameterType)).ToList();
                        parameters.Insert(0, obj);
                        methodInfo.Invoke(service, parameters.ToArray());
                    }
                    else
                    {
                        var parameters = assert.args.ConvertAll(arg => Convert.ChangeType(arg, methodInfo.GetParameters()[0].ParameterType)).ToList();
                        parameters.Insert(0, obj);
                        methodInfo.Invoke(service, parameters.ToArray());
                    }

                    // Проверка исключения
                    if (assert.exception != null)
                    {
                        throw new Exception($"Expected exception '{assert.exception}' was not thrown.");
                    }
                }
                catch (TargetInvocationException ex)
                {
                    // Проверка на соответствие имени исключения
                    if (assert.exceptions != null && assert.exceptions[i] != null && ex.InnerException?.GetType().Name != assert.exceptions[i])
                    {
                        throw new Exception($"Expected exception '{assert.exceptions[i]}' index of {i}, but got '{ex.InnerException?.GetType().Name}'.");
                    }
                    if (assert.exception != null && ex.InnerException?.GetType().Name != assert.exception)
                    {
                        throw new Exception($"Expected exception '{assert.exception}', but got '{ex.InnerException?.GetType().Name}'.");
                    }
                }
            }
            else if (assert.function != null)
            {
                var methodInfo = typeof(Service).GetMethod(assert.function);
                if (methodInfo == null)
                {
                    throw new Exception($"Method {assert.function} not found in type {typeof(Service).Name}");
                }

                try
                {
                    object expected = null;
                    if (assert.result != null)
                    {
                        expected = assert.result;
                    }
                    else
                    {
                        try
                        {
                            expected = assert.results[i];
                        }
                        catch (NullReferenceException)
                        {
                            expected = null;
                        }
                    }

                    if (assert.args.Count > 0 && assert.args[0] is JArray)
                    {
                        var args = assert.args[i] as JArray;
                        var parameters = args.Select(arg => ((JToken)arg).ToObject(methodInfo.GetParameters()[0].ParameterType)).ToList();
                        parameters.Insert(0, obj);
                        var got = methodInfo.Invoke(service, parameters.ToArray());
                        AssertByValue(expected, got, assert.type_assert, i);

                    }
                    else
                    {
                        var parameters = assert.args.ConvertAll(arg => Convert.ChangeType(arg, methodInfo.GetParameters()[0].ParameterType)).ToList();
                        parameters.Insert(0, obj);

                        foreach (var param in parameters)
                        {
                            Console.WriteLine(param.GetType().FullName);
                        }
                        Console.WriteLine(assert.function);

                        var got = methodInfo.Invoke(service, parameters.ToArray());
                        AssertByValue(expected, got, assert.type_assert, i);
                    }

                    // Проверка исключения
                    if (assert.exception != null)
                    {
                        throw new Exception($"Expected exception '{assert.exception}' was not thrown.");
                    }
                }
                catch (TargetInvocationException ex)
                {
                    // Проверка на соответствие имени исключения
                    if (assert.exceptions != null && assert.exceptions[i] != null && ex.InnerException?.GetType().Name != assert.exceptions[i])
                    {
                        throw new Exception($"Expected exception '{assert.exceptions[i]}' index of {i}, but got '{ex.InnerException?.GetType().Name}'.");
                    }
                    if (assert.exception != null && ex.InnerException?.GetType().Name != assert.exception)
                    {
                        throw new Exception($"Expected exception '{assert.exception}', but got '{ex.InnerException?.GetType().Name}'.");
                    }
                }
            }
            else
            {
                throw new Exception("pony");
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
    }
}
