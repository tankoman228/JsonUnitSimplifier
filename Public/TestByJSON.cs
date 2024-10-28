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
                            Assertion.Assert<Class, Service>(item, service, a, i);
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
                                Assertion.Assert<Service>(service, a, i);
                            }
                        }
                        else
                            Assertion.Assert<Service>(service, a, 0);
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
                            Assertion.Assert(item, a, i);
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
                        Assertion.Assert(item, a, i);
                    }
                    catch (Exception ex)
                    {
                        throw new Exception($"Assert of {a.type_assert} {a.function}{a.method}{a.field} dataset[{i}] failed: {ex.Message}\n{ex.StackTrace}\n{ex.InnerException?.StackTrace}");
                    }
                }
                i++;
            }
        }       
    }
}
