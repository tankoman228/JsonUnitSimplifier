using Microsoft.VisualStudio.TestTools.UnitTesting;
using JsonUnitSimplifier;
using System;
using System.IO;

namespace JsonUnitSimplifier__UNIT_TESTS_FOR_LIBRARY
{
    [TestClass]
    public class LayeredService_Testing
    {
        [TestMethod]
        public void LayeredService_Simple()
        {
            string content = File.ReadAllText( 
                PATHS.JSON +
                "MVVM.json");

            TestByJSON.TestLayeredService<TestedObject, TestedService>(content, new TestedService(new TestedRepositoryMock()),
                (a, b) => { a.Add(b); }, (a, b) => {});
        }
    }
}