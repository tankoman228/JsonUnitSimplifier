using System;

namespace JsonUnitSimplifier
{
    [AttributeUsage(AttributeTargets.Method)]
    public class GenerateFunctionAttribute : Attribute
    {
        public string Name { get; }

        public GenerateFunctionAttribute(string name)
        {
            Name = name;
        }
    }
}