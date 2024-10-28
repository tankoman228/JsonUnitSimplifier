using Microsoft.VisualStudio.TestTools.UnitTesting;
using JsonUnitSimplifier;
using System;
using System.IO;

namespace JsonUnitSimplifier__UNIT_TESTS_FOR_LIBRARY
{
    [TestClass]
    public class AutomaticTesting
    {
        [TestMethod]
        public void AutoTest()
        {
            string content = PATHS.JSON + "Auto\\Auto.json";
            TestByJSON.AutoTestByJSON(content);
        }

        [TestMethod]
        public void AutoTests()
        {
            string content = PATHS.JSON + "Auto\\";
            TestByJSON.AutoTestByJSONs(content);
        }
    }
}