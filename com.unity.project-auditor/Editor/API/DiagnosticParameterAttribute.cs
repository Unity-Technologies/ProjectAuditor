using System;

namespace Unity.ProjectAuditor.Editor
{
    [AttributeUsage(AttributeTargets.Field)]
    public class DiagnosticParameterAttribute : Attribute
    {
        public string Name { get; private set; }
        public int DefaultValue { get; private set; }

        public DiagnosticParameterAttribute(string name, int defaultValue)
        {
            Name = name;
            DefaultValue = defaultValue;
        }
    }
}
