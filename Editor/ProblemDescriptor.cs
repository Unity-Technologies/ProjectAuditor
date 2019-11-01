using System;

namespace Unity.ProjectAuditor.Editor
{
    [Serializable]
    public class ProblemDescriptor
    {
        public int id;
        public string type;
        public string method;
        public string value;
        public string customevaluator;
        public string area;
        public string problem;
        public string solution;

        public string description
        {
            get
            {
                return type + "." + method;
            }
        }
    }
}