﻿using System;
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

        /// <summary>
        /// Загрузка функций в словарь по GenerateFunctionAttribute
        /// </summary>
        static GenerateFunctions()
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();

            foreach (var assembly in assemblies)
            {
                Type[] types;

                try
                {
                    types = assembly.GetTypes();
                }
                catch (ReflectionTypeLoadException ex)
                {
                    // Логируем ошибки загрузки типов и продолжаем
                    foreach (var loaderException in ex.LoaderExceptions)
                    {
                        Console.WriteLine($"Loader exception: {loaderException.Message}");
                    }
                    continue; // Переходим к следующему assembly
                }

                foreach (var type in types)
                {
                    MethodInfo[] methods;

                    try
                    {
                        methods = type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                    }
                    catch (Exception ex)
                    {
                        // Логируем ошибку при получении методов и продолжаем
                        Console.WriteLine($"Error retrieving methods for {type.Name}: {ex.Message}");
                        continue; // Переходим к следующему type
                    }

                    foreach (var method in methods)
                    {
                        var attribute = method.GetCustomAttribute<GenerateFunctionAttribute>();
                        if (attribute != null)
                        {
                            try
                            {
                                var func = (Func<int, object>)Delegate.CreateDelegate(typeof(Func<int, object>), method);
                                Functions[attribute.Name] = func;

                                Console.WriteLine($"Function '{attribute.Name}' added from {type.Name}.{method.Name}.");
                            }
                            catch (Exception ex)
                            {
                                // Логируем ошибку создания делегата
                                Console.WriteLine($"Error creating delegate for {method.Name}: {ex.Message}");
                            }
                        }
                    }
                }
            }
        }

    }
}