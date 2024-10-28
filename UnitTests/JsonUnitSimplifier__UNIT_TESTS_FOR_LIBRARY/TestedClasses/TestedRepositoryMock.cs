using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JsonUnitSimplifier__UNIT_TESTS_FOR_LIBRARY
{
    internal class TestedRepositoryMock : ITestedRepository
    {
        private List<TestedObject> _objects = new List<TestedObject>();

        public void Delete(int id)
        {
            _objects.RemoveAt(id);
        }

        public int Save(TestedObject repository)
        {
            _objects.Add(repository);
            return _objects.Count - 1;
        }

        public TestedObject[] SelectAll()
        {
            return _objects.ToArray();
        }

        public void Update(TestedObject testedObject, int id)
        {
            _objects[id] = testedObject;
        }
    }
}
