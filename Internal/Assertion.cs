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
    /// <summary>
    /// Выполнение проверок и выброс исключений в случае, если они проваливаются
    /// 
    /// Примечание: 
    /// i - номер объекта в датасете
    /// </summary>
    internal class Assertion
    {
        /// <summary>
        /// Выполнение ассерта (проверки), описанного в юнит-тесте (см. файл JSON)
        /// Если не прошёл проверку - исключение
        /// </summary>
        /// <param name="obj">Какой объект проверяем</param>
        /// <param name="assert">Проверка из JSON файла</param>
        /// <param name="i">Номер объекта в датасете</param>
        /// <exception cref="Exception"></exception>
        internal static void Assert<T>(T obj, Assert assert, int i)
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

            else throw new ArgumentException("Unknown assert type error");
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
        internal static void Assert<Class, Service>(Class obj, Service service, Assert assert, int i)
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
                throw new Exception("Assert of service-to-object must be function or method!");
            }
        }


        /// <summary>
        /// Проверка вызова функции, ожидаемого результата. Если isFunction false, тогда простой вызов метода
        /// object first_arg нужен для Assert<Class, Service>, вставится как первый аргумент, помимо остальных
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
                    ExceptionBuilder.ThrowWithFullInfo($"Expected exception '{expectedException}', i = {i}, but got ", ex);
                }
            }
            catch (Exception ex)
            {
                ExceptionBuilder.ThrowWithFullInfo($"FATAL ERROR in assert at {i} calling '{fname}'", ex);
            }
        }


        /// <summary>
        ///  Вспомогательный метод для получения значения поля у объекта (написан ИИ)
        ///  Используется в Assert<T>(T obj, Assert assert, int i)
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


        /// <summary>
        ///  Обычная проверка по значению и type_assert
        /// </summary>
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
            parameters = paramList.ToArray(); 
        }
    }
}
