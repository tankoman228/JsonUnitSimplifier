using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace JsonUnitSimplifier
{
    /// <summary>
    /// Генерирует датасет к юнит-тесту
    /// </summary>
    internal class DatasetCreator
    {

        /// <summary>
        /// Генерация датасета согласно указанным в JSON параметрам
        /// </summary>
        /// <typeparam name="T">Тип данных датасета</typeparam>
        /// <param name="test">Класс юннит-теста (получаемый после парсинга файла)</param>
        /// <exception cref="InvalidCastException"></exception>
        internal static T[] getDataset<T>(UnitTest test)
        {
            if (test.rules.Count == 0)
                return new T[0];

            // Правила генерации для разных режимов
            List<Func<int, object>> rules_constructor = new List<Func<int, object>>();
            Dictionary<string, Func<int, object>> fields_constructor = new Dictionary<string, Func<int, object>>();

            // Определение правил (парсинг из JSON'а)
            if (test.mode == "fields") //Объекты T создавать не конструктором, а задавая поля
            {
                foreach (var rule in test.rules)
                {
                    fields_constructor.Add(rule.field, get_generation_rule(rule, typeof(T)));
                }
            }
            else if (test.mode == "constructor") //Объекты создавать конструктором, который соответствует функциям
            {
                foreach (var rule in test.rules)
                {
                    rules_constructor.Add(get_generation_rule(rule, typeof(T)));
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

                            combinations[j, i] = rules_constructor[i](current_arg);
                        }
                    }

                    // По массиву объекты * аргументы создаём сам датасет
                    for (int i = 0; i < combinationsCount; i++)
                    {
                        var parameters = new object[test.rules.Count];
                        for (int j = 0; j < test.rules.Count; j++)
                        {
                            parameters[j] = combinations[i, j];
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

                            combinations[j].Add(test.rules[i].field, fields_constructor[test.rules[i].field](current_arg));
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
                        var parameters = rules_constructor.Select(rule => rule(i)).ToArray();

                        dataset[i] = (T)Activator.CreateInstance(typeof(T), parameters);
                    }
                }
                else if (test.mode == "fields") // Аналогично с полями
                {
                    for (int i = 0; i < combinationsCount; i++)
                    {
                        var instance = Activator.CreateInstance(typeof(T));
                        foreach (var kvp in fields_constructor)
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
        /// Преобразует указанное правило генерации датасета в функцию с приведением типов
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


        /// <summary>
        /// Преобразует указанное правило генерации датасета в функцию. Без приведения типов
        /// </summary>
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
        

        /// <summary>
        /// Пытается получить тип значений, генерируемых правилом. Не получится - вернёт нуль
        /// </summary>
        private static Type GetTypeOfRule(Rule rule, Type DatasetType)
        {
            Type type = null;

            if (rule.field_type != null) // User sets the type
                type = Type.GetType(rule.field_type);
            else if (rule.field != null) // Auto-finding type
            {
                var field = DatasetType.GetField(rule.field);
                var prop = DatasetType.GetProperty(rule.field);

                if (field != null) return field.FieldType;
                else if (prop != null) return prop.PropertyType;
            }
            return type;
        }

    }
}
