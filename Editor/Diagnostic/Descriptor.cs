using System;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine.Serialization;

namespace Unity.ProjectAuditor.Editor.Diagnostic
{
    /// <summary>
    /// Descriptor defines the problem and a possible recommendation
    /// </summary>
    [Serializable]
    public sealed class Descriptor : IEquatable<Descriptor>
    {
        /// <summary>
        /// An unique identifier for the diagnostic. IDs must have exactly 3 upper case characters, followed by 4 digits
        /// </summary>
        public string Id;

        /// <summary>
        /// Diagnostic title
        /// </summary>
        public string Title;

        /// <summary>
        /// Message used to describe a specific instance of the diagnostic
        /// </summary>
        public string MessageFormat;

        /// <summary>
        /// Default severity of the diagnostic
        /// </summary>
        public Severity DefaultSeverity;

        /// <summary>
        /// Returns true if the diagnostic is enabled by default.
        /// </summary>
        public bool IsEnabledByDefault = true;

        /// <summary>
        /// Affected areas
        /// </summary>
        public string[] Areas;

        /// <summary>
        /// Affected platforms. If null, the diagnostic applies to all platforms
        /// </summary>
        public string[] Platforms;

        /// <summary>
        /// Description of the diagnostic
        /// </summary>
        public string Description;

        /// <summary>
        /// Recommendation to fix the diagnostic
        /// </summary>
        public string Solution;

        /// <summary>
        /// Url to documentation
        /// </summary>
        public string DocumentationUrl;

        /// <summary>
        /// Minimum Unity version this diagnostic applies to. If not specified, the diagnostic applies to all versions
        /// </summary>
        public string MinimumVersion;

        /// <summary>
        /// Maximum Unity version this diagnostic applies to. If not specified, the diagnostic applies to all versions
        /// </summary>
        public string MaximumVersion;

        /// <summary>
        /// Optional Auto-fixer
        /// </summary>
        [JsonIgnore]
        public Action<ProjectIssue> fixer;

        /// <summary>
        /// Name of the type (namespace and class/struct) of a known code API issue
        /// </summary>
        public string Type;

        /// <summary>
        /// Name of the method of a known code API issue
        /// </summary>
        public string Method;

        /// <summary>
        /// The evaluated value of a know code API issue
        /// </summary>
        public string Value;

        [JsonConstructor]
        internal Descriptor()
        {
            // only for json serialization purposes
        }

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
            Id = id;
            Title = title;
            Areas = areas;
            MessageFormat = string.Empty;
            Description = description;
            Solution = solution;

            Type = string.Empty;
            Method = string.Empty;
            DefaultSeverity = Severity.Moderate;
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
            return Id == other.Id;
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
            issue.WasFixed = true;
        }

        /// <summary>Returns a hash code for the Descriptor.</summary>
        /// <description>More specifically, returns the hash code for the Descriptor's Issue ID.</description>
        /// <returns>The computed hash code.</returns>
        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }
    }
}
