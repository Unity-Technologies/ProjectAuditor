using System;

namespace Unity.ProjectAuditor.Editor
{
    [Serializable]
    public class ProblemDescriptor
    {
        // TODO: remove auditor-specific fields: method, type and customevaluator
        public int id;
        public string description;
        public string type;
        public string method;
        public string value;
        public string customevaluator;
        public string area;
        public string problem;
        public string solution;
        public Rule.Action action;
    }
}