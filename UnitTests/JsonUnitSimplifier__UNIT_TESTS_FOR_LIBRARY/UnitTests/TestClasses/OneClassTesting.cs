using Microsoft.VisualStudio.TestTools.UnitTesting;
using JsonUnitSimplifier;
using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace JsonUnitSimplifier__UNIT_TESTS_FOR_LIBRARY
{
    [TestClass]
    public class OneClassTesting
    {

        [TestMethod]
        public void Fields()
        {
            GenerateFunctions.AddFunc("created_function", i => $"Name {i}");
            int i = 0;
            var steps = new double[] { -1, -0.5, 0, 0.5, 1 };

            // Можно передать путь к JSON файлу
            TestByJSON.TestObject<TestedObject>(PATHS.JSON + "Fields.json", o => {
                Assert.AreEqual(i + 1, o.FieldInt);
                Assert.AreEqual(true, o.FieldFloat < 0);
                Assert.AreEqual(steps[i % 5], o.FieldDouble);
                Assert.AreEqual($"Name {i}", o.FieldString);
                i++;
            });
        }

        [TestMethod]
        public void Constructors()
        {
            GenerateFunctions.AddFunc("created_function", i => $"Name {i}");

            string content = File.ReadAllText(PATHS.JSON + "Constructors.json"); // Можно передать JSON строку

            int i = 0;
            var steps = new double[] { -1, -0.5, 0, 0.5, 1 };

            TestByJSON.TestObject<TestedObject>(content, o => {
                Assert.AreEqual(i - 2, o.FieldInt);
                Assert.AreEqual(-23.2f, o.FieldFloat);
                Assert.AreEqual(steps[i % 5], o.FieldDouble);
                i++;
            });
        }

        [TestMethod]
        public void MethodsAndFunctions()
        {
            GenerateFunctions.AddFunc("created_function", i => $"Name {i}");

            string content = PATHS.JSON + "Methods.json";
          
            TestByJSON.TestObject<TestedObject>(content, o => {
                o.SetSecret("he-he");
            });
        }

        [TestMethod]
        public void ExceptionsAsserts()
        {
            GenerateFunctions.AddFunc("created_function", i => $"Name {i}");

            string content = PATHS.JSON + "Exceptions.json";

            TestByJSON.TestObject<TestedObject>(content, o => {
                o.SetSecret("he-he");
            });
        }

        [TestMethod]
        public void CombinationModes()
        {
            GenerateFunctions.AddFunc("chicken", i => $"kik {i * 9}");

            string content = PATHS.JSON + "CombinationModeSimple.json";
            int dataset_size = 0;

            TestByJSON.TestObject<TestedObject>(content, o => {
                dataset_size++;
            });
            Assert.AreEqual(5, dataset_size);


            content = File.ReadAllText(PATHS.JSON + "CombinationModeAllToAll.json");
            dataset_size = 0;

            TestByJSON.TestObject<TestedObject>(content, o => {
                dataset_size++;
            });
            Assert.AreEqual(20, dataset_size);
        }



        static Random random = new Random();

        [GenerateFunction("emails")]
        static object Email(int i)
        {
            const string chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            const string domains = "gmail.com,yahoo.com,hotmail.com,example.com";

            int length = random.Next(20, 40);
            StringBuilder username = new StringBuilder(length);

            for (int j = 0; j < length; j++)
            {
                username.Append(chars[random.Next(chars.Length)]);
            }
            string[] domainList = domains.Split(',');
            string domain = domainList[random.Next(domainList.Length)];

            return $"{username}{i}@{domain}";
        }

        [GenerateFunction("emails_wrong")]
        static object EmailWrong(int i)
        {
            string answ = (string)Email(i);

            return answ.Replace("@", "@@");
        }

        [TestMethod]
        public void EmailCheck()
        {       
            var content = PATHS.JSON + "EmailCheckPositive.json";
            TestByJSON.TestObject<EmailVerificator>(content, o => {               
                Console.WriteLine(o.email);            
            });

            content = PATHS.JSON + "EmailCheckNegative.json";
            TestByJSON.TestObject<EmailVerificator>(content, o => {});
        }
    }
}