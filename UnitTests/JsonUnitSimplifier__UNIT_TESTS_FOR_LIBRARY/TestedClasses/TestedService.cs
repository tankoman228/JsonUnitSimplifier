using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JsonUnitSimplifier__UNIT_TESTS_FOR_LIBRARY
{
    internal class TestedService
    {
        ITestedRepository testedRepository;

        public TestedService(ITestedRepository testedRepository)
        {
            this.testedRepository = testedRepository;
        }

        public void Add(TestedObject testedObject)
        {
            testedRepository.Save(testedObject);
        }

        public int Len()
        {
            return testedRepository.SelectAll().Count();
        }

        public double GetDouble(TestedObject testedObject)
        {
            return testedObject.FieldDouble;
        }

        public void AddDouble(int id, double add)
        {
            testedRepository.SelectAll()[id].FieldDouble += add;
        }

        public void NullExcept()
        {
            throw new ArgumentNullException();
        }
    }
}
