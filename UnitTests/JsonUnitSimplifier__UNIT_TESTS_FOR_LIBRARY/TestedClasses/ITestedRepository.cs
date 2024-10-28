using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JsonUnitSimplifier__UNIT_TESTS_FOR_LIBRARY
{
    internal interface ITestedRepository
    {
        int Save(TestedObject repository);
        TestedObject[] SelectAll();
        void Update(TestedObject testedObject, int id);
        void Delete(int id);
    }
}
