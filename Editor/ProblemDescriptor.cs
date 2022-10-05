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
        /// Quality. For example, using deprecated APIs that might be removed in the future
        /// </summary>
        Quality,

        /// <summary>
        /// Lack of platform support. For example, using APIs that are not supported on a specific platform and might fail at runtime
        /// </summary>
        Support,

        /// <summary>
        /// Required by platform. Typically this issue must be fixed before submitting to the platform store
        /// </summary>
        Requirement
    }

    /// <summary>
    /// ProblemDescriptor defines the problem and a possible recommendation
    /// </summary>
    [Serializable]
    public sealed class ProblemDescriptor : IEquatable<ProblemDescriptor>
    {
        /// <summary>
        /// An unique identifier for the diagnostic
        /// </summary>
        public string id;

        /// <summary>
        /// Diagnostic title
        /// </summary>
        public string title;

        /// <summary>
        /// Message used to describe a specific instance of the diagnostic
        /// </summary>
        public string messageFormat;

        /// <summary>
        /// Default severity of the diagnostic
        /// </summary>
        public Rule.Severity severity;

        /// <summary>
        /// Affected areas
        /// </summary>
        public string[] areas;

        /// <summary>
        /// Affected platforms. If null, the diagnostic applies to all platforms
        /// </summary>
        public string[] platforms;

        /// <summary>
        /// Description of the diagnostic
        /// </summary>
        public string description;

        /// <summary>
        /// Recommendation to fix the diagnostic
        /// </summary>
        public string solution;

        /// <summary>
        /// Url to documentation
        /// </summary>
        public string documentation;

        /// <summary>
        /// Minimum Unity version this diagnostic applies to. If not specified, the diagnostic applies to all versions
        /// </summary>
        public string minimumVersion;

        /// <summary>
        /// Maximum Unity version this diagnostic applies to. If not specified, the diagnostic applies to all versions
        /// </summary>
        public string maximumVersion;

        /// <summary>
        /// Optional Auto-fixer
        /// </summary>
        public Action<ProjectIssue> fixer;

        public bool critical;

        // TODO: remove auditor-specific fields
        public string customevaluator;
        public string type;
        public string method;
        public string value;

        internal ProblemDescriptor(string id, string title, string[] areas, string description = null, string solution = null)
        {
            this.id = id;
            this.title = title;
            this.areas = areas;
            this.messageFormat = "{0}";
            this.description = description;
            this.solution = solution;

            type = string.Empty;
            method = string.Empty;
            critical = false;
        }

        public ProblemDescriptor(string id, string title, Area area, string description = null, string solution = null)
            : this(id, title, new[] {area.ToString()}, description, solution)
        {
        }

        public ProblemDescriptor(string id, string title, Area[] areas, string description = null, string solution = null)
            : this(id, title, areas.Select(a => a.ToString()).ToArray(), description, solution)
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

        public bool IsValid()
        {
            return !string.IsNullOrEmpty(id);
        }
    }
}
