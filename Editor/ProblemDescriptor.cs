using System;

namespace Unity.ProjectAuditor.Editor
{
    /// <summary>
    /// Affected area
    /// </summary>
    public enum Area
    {
        /// <summary>
        /// CPU Performance
        /// </summary>
        CPU,

        /// <summary>
        /// GPU Performance
        /// </summary>
        GPU,

        /// <summary>
        /// Memory consumption
        /// </summary>
        Memory,

        /// <summary>
        /// Application size
        /// </summary>
        BuildSize,

        /// <summary>
        /// Build time
        /// </summary>
        BuildTime,

        /// <summary>
        /// Import time
        /// </summary>
        ImportTime,

        /// <summary>
        /// Load times
        /// </summary>
        LoadTimes
    }

    /// <summary>
    /// ProblemDescriptor defines the problem and a possible recommendation.
    /// </summary>
    [Serializable]
    public class ProblemDescriptor : IEquatable<ProblemDescriptor>
    {
        public Rule.Severity severity;
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
        public string minimumVersion;
        public string maximumVersion;

        public ProblemDescriptor(int id, string description, string area, string problem = null, string solution = null)
        {
            this.id = id;
            this.description = description;
            this.area = area;
            this.problem = problem;
            this.solution = solution;

            type = string.Empty;
            method = string.Empty;
            critical = false;
        }

        public ProblemDescriptor(int id, string description, Area area, string problem = null, string solution = null)
            : this(id, description, area.ToString(), problem, solution)
        {
        }

        public ProblemDescriptor(int id, string description, Area[] areas, string problem = null, string solution = null)
            : this(id, description, string.Join("|", areas), problem, solution)
        {
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
            return obj.GetType() == GetType() && Equals((ProblemDescriptor)obj);
        }

        public override int GetHashCode()
        {
            return id;
        }
    }
}
