using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace JsonUnitSimplifier
{
    /// <summary>
    /// Генератор случайных строк. Формат template см. в README
    /// </summary>
    public class RandomStringGenerator
    {
        private static readonly Random _random = new Random();

        public string Generate(string template)
        {
            template = Regex.Replace(template, @"\[([aAcdDlLKR]{1})(\d+)?(-(\d+))?\]", m =>
            {
                char type = m.Groups[1].Value[0];
                int min = m.Groups[2].Success ? int.Parse(m.Groups[2].Value) : 1;
                int max = m.Groups[4].Success ? int.Parse(m.Groups[4].Value) : min;

                int count = _random.Next(min, max + 1);
                return GenerateRandomCharacters(type, count);
            });

            template = Regex.Replace(template, @"\{([^}]+)\}", m =>
            {
                var options = m.Groups[1].Value.Split(',');
                return options[_random.Next(options.Length)];
            });

            return template;
        }

        private string GenerateRandomCharacters(char type, int count)
        {
            string result = string.Empty;

            for (int i = 0; i < count; i++)
            {
                char randomChar;
                switch (type)
                {
                    // любой символ
                    case 'a': randomChar = (char)_random.Next(1, 127); break;

                    // любая цифра или буква в нижнем регистре
                    case 'c': randomChar = _random.Next(2) == 0 ? (char)_random.Next('0', '9' + 1) : (char)_random.Next('a', 'z' + 1); break;

                    // любая цифра
                    case 'd': randomChar = (char)_random.Next('0', '9' + 1); break;

                    // любая буква в нижнем регистре
                    case 'l': randomChar = (char)_random.Next('a', 'z' + 1); break;

                    // любая буква в верхнем регистре
                    case 'L': randomChar = (char)_random.Next('A', 'Z' + 1); break;

                    // любая буква в случайном регистре
                    case 'R': randomChar = 
                            _random.Next(2) == 0 ? (char)_random.Next('a', 'z' + 1) : (char)_random.Next('A', 'Z' + 1); break;

                    // любая буква в случайном регистре или цифра
                    case 'K': randomChar = 
                            _random.Next(2) == 0 ? (char)_random.Next('a', 'z' + 1) : 
                            _random.Next(2) == 0 ? (char)_random.Next('A', 'Z' + 1) : 
                                                   (char)_random.Next('0', '9' + 1); break; 
                    
                        default: throw new ArgumentOutOfRangeException("Not valid random char, read README.md about random templates");
                }

                result += randomChar;
            }

            return result;
        }
    }

}
