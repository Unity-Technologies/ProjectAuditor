using System;

namespace Unity.ProjectAuditor.Editor
{
    [Serializable]
    public class ProblemDescriptor : IEquatable<ProblemDescriptor>
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

        public bool Equals(ProblemDescriptor other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return id == other.id;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj.GetType() == this.GetType() && Equals((ProblemDescriptor)obj);
        }

        public override int GetHashCode()
        {
            return id;
        }
    }
}
