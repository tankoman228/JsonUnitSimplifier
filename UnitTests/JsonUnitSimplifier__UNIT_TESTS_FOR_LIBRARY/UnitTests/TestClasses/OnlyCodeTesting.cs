using Microsoft.VisualStudio.TestTools.UnitTesting;
using JsonUnitSimplifier;
using System;

namespace JsonUnitSimplifier__UNIT_TESTS_FOR_LIBRARY
{
    [TestClass]
    public class OnlyCodeTesting
    {
        [TestMethod]
        public void Test()
        {
            // ������� �������� �������������
            var dataset_array = 
                Constructor.CreateByTemplate<TestedObject>(10, x => new TestedObject { FieldInt = x * 3 });
        }
    }
}