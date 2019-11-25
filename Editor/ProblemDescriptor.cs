using System;

namespace Unity.ProjectAuditor.Editor
{
    [Serializable]
    public class ProblemDescriptor
    {
        public int id;
        public string opcode;
        public string type;
        public string method;
        public string value;
        public string customevaluator;
        public string area;
        public string problem;
        public string solution;
        public Rule.Action action;

        public string description
        {
            get
            {
                if (!string.IsNullOrEmpty(opcode))
                    return opcode;
                return type + "." + method;
            }
        }
    }
}