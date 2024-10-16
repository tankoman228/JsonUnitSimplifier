using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JsonUnitSimplifier
{
    internal class JSON_Parser
    {
        internal static UnitTest Parse(string json)
        {
            return JsonConvert.DeserializeObject<UnitTest>(json);
        }
    }

    internal class UnitTest
    {
        public string id { get; set; }
        public string mode { get; set; }
        public string combination_mode { get; set; }
        public List<string> classes { get; set; }
        public List<Rule> rules { get; set; }
        public List<Assert> assert_before_lambda { get; set; }
        public List<Assert> assert_after_lambda { get; set; }
    }

    internal class Rule
    {
        public string field { get; set; }
        public List<int> values { get; set; }
        public string value { get; set; }
        public List<int> range { get; set; }
        public int? step { get; set; }
        public string function { get; set; }

        public int Combinations { get 
            {
                if (value != null)
                    return 1;
                if (range != null && step != null)
                    return (range[1] - range[0]) + 1;
                if (values != null)
                    return values.Count;
                if (function != null)
                    return 1;
                throw new Exception("I am a teapot");
            } 
        }
    }

    internal class Assert
    {
        public string method { get; set; }
        public string function { get; set; }
        public string target { get; set; }
        public List<object> args { get; set; }
        public string exception { get; set; }
        public List<object> results { get; set; }
        public object result { get; set; }
        public string type_assert { get; set; }
        public string field { get; set; }
        public object value { get; set; }
        public List<object> values { get; set; }
    }
}
