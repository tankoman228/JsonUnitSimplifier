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
            Class[] dataset = DatasetCreator.getDataset<Class>(test); // И сам датасет
            foreach (var item in dataset)
            {
                insert(service, item);
            }

            if (test.assert_before_lambda != null)
                goTestLayeredServiceAsserts(test.assert_before_lambda, dataset, service);

            if (testLogic != null)
                testLogic(service, dataset);

            if (test.assert_after_lambda != null)
                goTestLayeredServiceAsserts(test.assert_after_lambda, dataset, service);

        }
        private static void goTestLayeredServiceAsserts<Class, Service>(
            List<Assert> asserts, Class[] dataset, Service service
            )
        {
            foreach (var a in asserts)
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
                        if (a.args.Count > 0 && a.args[0] is JArray)
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
            Class[] dataset = DatasetCreator.getDataset<Class>(test); // И сам датасет
            Console.WriteLine(test.id + " start as a usual test");

            // Ассерты до доп. логики
            if (test.assert_before_lambda != null)
            {
                GoTestClassAsserts(test.assert_before_lambda, dataset);
            }

            // Доп. логика
            foreach (var item in dataset)
            {
                testLogic(item);
            }

            // Ассерты после доп. логики
            if (test.assert_after_lambda != null)
            {
                GoTestClassAsserts(test.assert_after_lambda, dataset);
            }
        }
        private static void GoTestClassAsserts<T>(List<Assert> asserts, T[] dataset)
        {
            int i = 0;
            foreach (var item in dataset)
            {
                foreach (var a in asserts)
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
