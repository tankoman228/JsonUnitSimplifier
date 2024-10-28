using Microsoft.VisualStudio.TestTools.UnitTesting;
using JsonUnitSimplifier;
using System;
using System.IO;
using System.Text.RegularExpressions;

namespace JsonUnitSimplifier__UNIT_TESTS_FOR_LIBRARY
{
    [TestClass]
    public class RandomStringTest
    {
        [TestMethod]
        public void RandomString()
        {
            RandomStringGenerator generator = new RandomStringGenerator();
            Assert.IsTrue(Regex.IsMatch(generator.Generate(""), @""));
            Assert.IsTrue(Regex.IsMatch(generator.Generate("[d3]"), @"\d\d\d"));
            Assert.IsTrue(Regex.IsMatch(generator.Generate("[c3]"), @"\w\w\w"));
            Assert.IsTrue(Regex.IsMatch(generator.Generate("[l3]"), @"\D\D\D"));

            for (int i = 0; i < 10; i++)
            {
                Console.WriteLine(generator.Generate("a [a5-15]]").Replace("\n", ""));
                Console.WriteLine(generator.Generate("L [L0-3]]".Replace("\n", "")));
                Console.WriteLine(generator.Generate("R [R0-3]]".Replace("\n", "")));
                Console.WriteLine(generator.Generate("K [K0-3]]".Replace("\n", "")));
                Console.WriteLine();
            }
            for (int i = 0; i < 10; i++)
            {
                Console.WriteLine(generator.Generate("[c5-15]@{gmail.com,mail.ru}"));
            }    
            for (int i = 0; i < 10; i++)
            {
                Console.WriteLine(generator.Generate("+7 ([d3]) [d3]-[d2]-[d2]"));
            }            
        }
    }
}