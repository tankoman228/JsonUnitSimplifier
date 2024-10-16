using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace JsonUnitSimplifier
{
    public class TestByJSON
    {
        public static void AutoTestByJSON<Class>(
        string json_path
        )
        {
            throw new NotImplementedException();
        }

        public static void AutoTestByJSONs<Class>(
            string[] json_paths
            )
        {
            throw new NotImplementedException();
        }

        public static void AutoTestByJSONs<Class>(
            string jsons_path
            )
        {
            throw new NotImplementedException();
        }

        public static void TestServiceMVVM<Class, Service>(
            string json,
            Service service,
            Action<Service, Class> insert,
            Action<Service, Class> testLogic
            )
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="Class"></typeparam>
        /// <param name="json"></param>
        /// <param name="testLogic"></param>
        public static void TestObject<Class>(
                string json,
                Action<Class> testLogic
            )
        {
            var test = JSON_Parser.Parse(json);

            Class[] dataset = getDataset<Class>(test);
        }

        private static T[] getDataset<T>(UnitTest test)
        {

            List<Func<int, object>> generation_rules_l = new List<Func<int, object>>();
            Dictionary<string, Func<int, object>> generation_rules_d = new Dictionary<string, Func<int, object>>();

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


            if (test.combination_mode == "all-to-all")
            {
                int combinationsCount = 1;
                foreach (var rule in test.rules)
                {
                    combinationsCount *= rule.Combinations;
                }

                T[] dataset = new T[combinationsCount];

                if (test.mode == "constructor")
                {
                    var combinations = new object[combinationsCount, test.rules.Count];

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
                else if (test.mode == "fields")
                {
                    var combinations = new Dictionary<string, object>[combinationsCount];

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

                            combinations[i].Add(test.rules[i].field, generation_rules_l[i](current_arg));
                        }
                    }

                    for (int i = 0; i < combinationsCount; i++)
                    {
                        var instance = Activator.CreateInstance(typeof(T));
                        foreach (var key in combinations[i].Keys)
                        {
                            var fieldValue = combinations[i][key];
                            var fieldInfo = typeof(T).GetField(key, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                            fieldInfo.SetValue(instance, fieldValue);
                        }
                        dataset[i] = (T)instance;
                    }
                }
                else { throw new InvalidCastException(); }

                return dataset;
            }
            else if (test.combination_mode == "simple")
            {
                int combinationsCount = 1;
                foreach (var rule in test.rules)
                {
                    if (combinationsCount < rule.Combinations)
                        combinationsCount = rule.Combinations;
                }

                T[] dataset = new T[combinationsCount];

                if (test.mode == "constructor")
                {
                    for (int i = 0; i < combinationsCount; i++)
                    {
                        var parameters = generation_rules_l.Select(rule => rule(i)).ToArray();
                        dataset[i] = (T)Activator.CreateInstance(typeof(T), parameters);
                    }
                }
                else if (test.mode == "fields")
                {
                    for (int i = 0; i < combinationsCount; i++)
                    {
                        var instance = Activator.CreateInstance(typeof(T));
                        foreach (var kvp in generation_rules_d)
                        {
                            var fieldValue = kvp.Value(i);
                            var fieldInfo = typeof(T).GetField(kvp.Key, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
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

        private static Func<int, object> get_generation_rule(Rule rule)
        {
            if (rule.values != null)
            {
                return i => rule.values[i % rule.values.Count];
            }
            else if (rule.value != null)
            {
                return i => rule.value;
            }
            if (rule.range != null)
            {
                return i => rule.value;
            }
            else if (rule.function != null)
            {
                return GenerateFunctions.Get(rule.function);
            }
            else
            {
                throw new Exception("Читай документацию, простофиля");
            }
        }
    }
}
