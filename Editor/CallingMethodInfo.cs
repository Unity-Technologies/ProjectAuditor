using System.Collections.Generic;

namespace Unity.ProjectAuditor.Editor
{
    public class CallingMethodInfo
    {
        public string name;
        public List<CallingMethodInfo> parents;

        public CallingMethodInfo(string _name)
        {
            name = _name;
            parents = new List<CallingMethodInfo>();
        }
    }
}