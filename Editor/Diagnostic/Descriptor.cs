using System;
using System.Linq;

namespace Unity.ProjectAuditor.Editor.Diagnostic
{
    /// <summary>
    /// Descriptor defines the problem and a possible recommendation
    /// </summary>
    [Serializable]
    public sealed class Descriptor : IEquatable<Descriptor>
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
        public Severity severity;

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
        public string documentationUrl;

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

        public Priority priority;

        // TODO: remove auditor-specific fields
        public string customevaluator;
        public string type;
        public string method;
        public string value;

        internal Descriptor(string id, string title, string[] areas, string description = null, string solution = null)
        {
            this.id = id;
            this.title = title;
            this.areas = areas;
            this.messageFormat = "{0}";
            this.description = description;
            this.solution = solution;

            type = string.Empty;
            method = string.Empty;
            priority = Priority.Normal;
        }

        public Descriptor(string id, string title, Area area, string description = null, string solution = null)
            : this(id, title, new[] {area.ToString()}, description, solution)
        {
        }

        public Descriptor(string id, string title, Area[] areas, string description = null, string solution = null)
            : this(id, title, areas.Select(a => a.ToString()).ToArray(), description, solution)
        {
        }

        public bool Equals(Descriptor other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return id == other.id;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj.GetType() == GetType() && Equals((Descriptor)obj);
        }

        public void Fix(ProjectIssue issue = null)
        {
            if (fixer != null)
            {
                fixer(issue);
                issue.wasFixed = true;
            }
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
