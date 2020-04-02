using System;

namespace Unity.ProjectAuditor.Editor
{
    [Serializable]
    public class ProblemDescriptor
    {
        public Rule.Action action;
        public string area;
        public string customevaluator;

        public string description;

        // TODO: remove auditor-specific fields: method, type and customevaluator
        public int id;
        public string method;
        public string problem;
        public string solution;
        public string type;
        public string value;
    }
}
