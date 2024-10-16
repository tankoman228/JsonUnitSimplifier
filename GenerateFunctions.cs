using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JsonUnitSimplifier
{
    public class GenerateFunctions
    {
        private static readonly Dictionary<string, Func<int, object>> Functions = new Dictionary<string, Func<int, object>>()
        {
            {"name", new Func<int, object>(i => { return 5; })},
            {"sirname", new Func<int, object>(i => { return 5; })},
            {"lastname", new Func<int, object>(i => { return 5; })},
            {"name+sirname+lastname", new Func<int, object>(i => { return 5; })},
            {"phone", new Func<int, object>(i => { return 5; })},
            {"email", new Func<int, object>(i => { return 5; })},
        };

        public static void AddFunc(string key, Func<int, object> func)
        {
            Functions.Add(key, func);
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
    }
}
