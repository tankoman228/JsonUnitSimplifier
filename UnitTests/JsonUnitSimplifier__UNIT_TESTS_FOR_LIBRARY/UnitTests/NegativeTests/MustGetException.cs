using Microsoft.VisualStudio.TestTools.UnitTesting;
using JsonUnitSimplifier;
using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace JsonUnitSimplifier__UNIT_TESTS_FOR_LIBRARY
{
    [TestClass]
    public class MustGetException
    {
        [TestMethod]
        public void FailAssert()
        {
            try
            {
                TestByJSON.TestObject<TestedObject>(PATHS.JSON_NEGATIVE + "AssertFail.json", o => {});
            }
            catch (Exception ex)
            {
                Assert.IsTrue(ex.Message.Contains("expected") && ex.Message.Contains("got"));
                Console.WriteLine(ex.Message);
                return;
            }
            Assert.Fail();
        }

        [TestMethod]
        public void FailDatasetCreate()
        {
            try
            {
                TestByJSON.TestObject<TestedObject>(PATHS.JSON_NEGATIVE + "FailDataset.json", o => { });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Assert.IsTrue(ex.Message.Contains("dataset"));
                return;
            }
            Assert.Fail();
        }

        [TestMethod]
        public void FailUnexpectedException()
        {
            try
            {
                TestByJSON.TestObject<TestedObject>(PATHS.JSON_NEGATIVE + "FailExceptions.json", o => { });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Assert.IsTrue(ex.Message.Contains("NotImplementedException"));
                Assert.IsTrue(ex.Message.Contains("ExceptNotImplementedIfZeroOrReturnZero"));
                return;
            }
            Assert.Fail();
        }

        [TestMethod]
        public void FailAutomatic()
        {
            int totalFiles = Directory.GetFiles(PATHS.JSON_NEGATIVE).Length;
            try
            {
                TestByJSON.AutoTestByJSONs(PATHS.JSON_NEGATIVE);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Assert.IsTrue(ex.Message.Contains("NotImplementedException"));
                Assert.IsTrue(ex.Message.Contains("ExceptNotImplementedIfZeroOrReturnZero"));
                Assert.IsTrue(ex.Message.Contains("dataset"));
                Assert.IsTrue(ex.Message.Contains("expected") && ex.Message.Contains("got"));
                Assert.IsTrue(ex.Message.Contains($"{totalFiles}/{totalFiles}"));
                return;
            }
            Assert.Fail();
        }
    }
}