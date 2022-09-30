using System;
using System.Linq;

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
        /// Load times
        /// </summary>
        LoadTime,

        /// <summary>
        /// Quality. For example, using deprecated APIs that might be removed in the future.
        /// </summary>
        Quality,

        /// <summary>
        /// Lack of platform support. For example, using APIs that are not supported on a specific platform and might fail at runtime.
        /// </summary>
        Support,

        /// <summary>
        /// Required by platform. Typically this issue must be fixed before submitting to the platform store.
        /// </summary>
        Requirement
    }

    /// <summary>
    /// ProblemDescriptor defines the problem and a possible recommendation.
    /// </summary>
    [Serializable]
    public sealed class ProblemDescriptor : IEquatable<ProblemDescriptor>
    {
        // An unique identifier for the diagnostic
        public string id;
        public string description;
        public string messageFormat;

        public bool critical;
        public Rule.Severity severity;

        // affected areas
        public string[] areas;
        // affected platforms
        public string[] platforms;
        public string problem;
        public string solution;
        public string documentation;
        public Action<ProjectIssue> fixer;
        public string minimumVersion;
        public string maximumVersion;

        // TODO: remove auditor-specific fields
        public string customevaluator;
        public string type;
        public string method;
        public string value;

        internal ProblemDescriptor(string id, string description, string[] areas, string problem = null, string solution = null)
        {
            this.id = id;
            this.description = description;
            this.areas = areas;
            this.messageFormat = "{0}";
            this.problem = problem;
            this.solution = solution;

            type = string.Empty;
            method = string.Empty;
            critical = false;
        }

        public ProblemDescriptor(string id, string description, Area area, string problem = null, string solution = null)
            : this(id, description, new[] {area.ToString()}, problem, solution)
        {
        }

        public ProblemDescriptor(string id, string description, Area[] areas, string problem = null, string solution = null)
            : this(id, description, areas.Select(a => a.ToString()).ToArray(), problem, solution)
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

        public void Fix(ProjectIssue issue = null)
        {
            if (fixer != null)
                fixer(issue);
        }

        public override int GetHashCode()
        {
            return id.GetHashCode();
        }
    }
}
