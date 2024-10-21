using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace JsonUnitSimplifier
{
    public class GenerateFunctions
    {
        private static readonly Dictionary<string, Func<int, object>> Functions = new Dictionary<string, Func<int, object>>() { };

        public static void AddFunc(string key, Func<int, object> func)
        {
            Functions[key] = func;
        }

        public static Func<int, object> Get(string key)
        {
            return Functions[key];
        }

        public static string getKeys()
        {
            string keys = "";
            foreach (string key in Functions.Keys)
            {
                keys += key + " ";
            }
            return keys;
        }


        static GenerateFunctions()
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();

            foreach (var assembly in assemblies)
            {
                var types = assembly.GetTypes();

                foreach (var type in types)
                {
                    var methods = type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

                    foreach (var method in methods)
                    {
                        var attribute = method.GetCustomAttribute<GenerateFunctionAttribute>();
                        if (attribute != null)
                        {
                            var func = (Func<int, object>)Delegate.CreateDelegate(typeof(Func<int, object>), method);
                            Functions[attribute.Name] = func;

                            Console.WriteLine($"Function '{attribute.Name}' added from {type.Name}.{method.Name}.");
                        }
                    }
                }
            }
        }
    }
}
