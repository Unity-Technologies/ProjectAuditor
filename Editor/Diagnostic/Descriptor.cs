using System;
using System.Linq;
using System.Runtime.CompilerServices;

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
        public Severity defaultSeverity;

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

        // TODO: remove auditor-specific fields
        internal string type;
        internal string method;
        internal string value;

        /// <summary>
        /// Initializes and returns an instance of Descriptor
        /// </summary>
        /// <param name="id">The Issue ID string.</param>
        /// <param name="title">A short human-readable 'name' for the issue</param>
        /// <param name="areas">The areas affected by this issue (see the values in the Areas enum)</param>
        /// <param name="description">A description of the issue.</param>
        /// <param name="solution">Advice on how to resolve the issue.</param>
        public Descriptor(string id, string title, string[] areas, string description, string solution)
        {
            this.id = id;
            this.title = title;
            this.areas = areas;
            this.messageFormat = "{0}";
            this.description = description;
            this.solution = solution;

            type = string.Empty;
            method = string.Empty;
            defaultSeverity = Severity.Moderate;
        }

        /// <summary>
        /// Initializes and returns an instance of Descriptor
        /// </summary>
        /// <param name="id">The Issue ID string.</param>
        /// <param name="title">A short human-readable 'name' for the issue</param>
        /// <param name="area">The Area affected by this issue</param>
        /// <param name="description">A description of the issue.</param>
        /// <param name="solution">Advice on how to resolve the issue.</param>
        public Descriptor(string id, string title, Area area, string description, string solution)
            : this(id, title, new[] {area.ToString()}, description, solution)
        {
        }

        /// <summary>
        /// Initializes and returns an instance of Descriptor
        /// </summary>
        /// <param name="id">The Issue ID string.</param>
        /// <param name="title">A short human-readable 'name' for the issue</param>
        /// <param name="areas">The Areas affected by this issue</param>
        /// <param name="description">A description of the issue.</param>
        /// <param name="solution">Advice on how to resolve the issue.</param>
        public Descriptor(string id, string title, Area[] areas, string description, string solution)
            : this(id, title, areas.Select(a => a.ToString()).ToArray(), description, solution)
        {
        }

        /// <summary>Returns true if the Descriptor is equal to a given Descriptor, false otherwise.</summary>
        /// <param name="other">The Descriptor to compare equality with.</param>
        /// <returns>The result of the equality comparison.</returns>
        public bool Equals(Descriptor other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return id == other.id;
        }

        /// <summary>Returns true if the Descriptor is equal to a given object, false otherwise.</summary>
        /// <param name="obj">The object to compare equality with.</param>
        /// <returns>The result of the equality comparison.</returns>
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj.GetType() == GetType() && Equals((Descriptor)obj);
        }

        internal void Fix(ProjectIssue issue)
        {
            // Temp workaround for lost 'fixer' after domain reload
            if (fixer == null)
                return;

            fixer(issue);
            issue.wasFixed = true;
        }

        /// <summary>Returns a hash code for the Descriptor.</summary>
        /// <description>More specifically, returns the hash code for the Descriptor's Issue ID.</description>
        /// <returns>The computed hash code.</returns>
        public override int GetHashCode()
        {
            return id.GetHashCode();
        }

        /// <summary>
        /// Returns whether the Descriptor has a valid Issue ID
        /// </summary>
        /// <returns>False if the Issue ID string is null or empty. Otherwise, returns true.</returns>
        public bool IsValid()
        {
            return !string.IsNullOrEmpty(id);
        }
    }
}
