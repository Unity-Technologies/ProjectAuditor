using System.Collections.Generic;

namespace Unity.ProjectAuditor.Editor
{
    public class MethodInstance
    {
        public string name;
        public List<MethodInstance> parents;

        public MethodInstance(string _name)
        {
            name = _name;
            parents = new List<MethodInstance>();
        }
    }
}