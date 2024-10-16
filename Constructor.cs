using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JsonUnitSimplifier
{
    /// <summary>
    /// Автоматическое создание объектов по передаваемым правилам.
    /// </summary>
    public class Constructor
    {
        /// <summary>
        /// Создание объектов при помощи конструктора. Каждый аргумент конструктора 
        /// соответсвует функции generateRules[номер аргумента конструктора](номер объекта в массиве)
        /// </summary>
        /// <param name="n">Число генерируемых объектов</param>
        /// <param name="generateRules">функции, соответствующие аргументам конструктора</param>
        /// <returns>массив объектов класса T</returns>
        public static T[] CreateByArgs<T>(int n, Func<int, object>[] generateRules)
        {
            T[] array = new T[n];
            for (int i = 0; i < n; i++)
            {
                var args = new object[generateRules.Length];
                for (int j = 0; j < generateRules.Length; j++)
                {
                    args[j] = generateRules[j](i);
                }

                array[i] = (T)Activator.CreateInstance(typeof(T), args); // Вызов конструктора
            }
            return array;
        }

        /// <summary>
        /// Создание объектов при помощи функции
        /// </summary>
        /// <param name="n">Число генерируемых объектов</param>
        /// <param name="generateRules">функция создания объекта</param>
        /// <returns>массив объектов класса T</returns>
        public static T[] CreateByTemplate<T>(int n, Func<int, T> generateRules) where T : class, new()
        {
            T[] array = new T[n];
            for (int i = 0; i < n; i++)
            {
                array[i] = generateRules(i);
                if (array[i] == null)
                    throw new Exception("Generate rules returned null");
            }
            return array;
        }
    }
}
