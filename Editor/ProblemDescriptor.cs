using System;

namespace Unity.ProjectAuditor.Editor
{
    /// <summary>
    /// ProblemDescriptor defines the problem and a possible recommendation.
    /// </summary>
    [Serializable]
    public class ProblemDescriptor : IEquatable<ProblemDescriptor>
    {
        public Rule.Action action;
        public string area;
        public string customevaluator;

        public string description;

        // TODO: remove auditor-specific fields: method, type and customevaluator
        public int id;
        public string type;
        public string method;
        public string value;
        public bool critical;
        public string problem;
        public string solution;

        public ProblemDescriptor(int id, string description, Area area, string problem, string solution)
        {
            this.id = id;
            this.description = description;
            this.area = area.ToString();
            this.problem = problem;
            this.solution = solution;

            this.type = String.Empty;
            this.method = String.Empty;
            this.critical = false;
        }

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
