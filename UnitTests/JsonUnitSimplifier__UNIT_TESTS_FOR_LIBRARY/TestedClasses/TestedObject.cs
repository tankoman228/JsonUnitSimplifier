using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JsonUnitSimplifier__UNIT_TESTS_FOR_LIBRARY
{
    internal class TestedObject
    {
        public int FieldInt;
        public float FieldFloat { get; set; }
        public double FieldDouble { get; set; }
        public string FieldString { get; set; }
        
        private string Secret { get; set; }

        public TestedObject() {}
        public TestedObject(int i, float f, double d, string s)
        {
            FieldInt = i;
            FieldFloat = f;
            FieldDouble = d;
            FieldString = s + "!";
        }

        public void SetSecret(String secret)
        {
            Secret = secret + "!!!";
        }
        public string GetSecret() => Secret;
        public string GetSecret2(int g) => (FieldInt + Secret) + g;

        public int ExceptNotImplementedIfZeroOrReturnZero(int f)
        {
            if (f == 0)
                return 0;

            throw new NotImplementedException();
        }
    }
}
