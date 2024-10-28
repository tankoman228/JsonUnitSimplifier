using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace JsonUnitSimplifier__UNIT_TESTS_FOR_LIBRARY
{
    internal class EmailVerificator
    {
        public string email;
        private static Regex reg = new Regex(@"^[a-zA-Z0-9]+([-._][a-zA-Z0-9]+)*@[a-zA-Z0-9]+([-.][a-zA-Z0-9]+)*\.[a-zA-Z]{2,}$");
        public EmailVerificator(string Email)
        {
            email = Email;
        }

        public bool Check()
        {
            return email != null && reg.IsMatch(email);
        }
    }
}
