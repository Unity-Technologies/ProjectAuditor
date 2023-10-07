using System;
using System.Linq;
using System.Runtime.CompilerServices;
using Newtonsoft.Json;

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
        [JsonRequired]
        public string id;

        /// <summary>
        /// Diagnostic title
        /// </summary>
        [JsonRequired]
        public string title;

        /// <summary>
        /// Message used to describe a specific instance of the diagnostic
        /// </summary>
        public string messageFormat;

        /// <summary>
        /// Default severity of the diagnostic
        /// </summary>
        [JsonRequired]
        public Severity defaultSeverity;

        /// <summary>
        /// Affected areas
        /// </summary>
        [JsonRequired]
        public string[] areas;

        /// <summary>
        /// Affected platforms. If null, the diagnostic applies to all platforms
        /// </summary>
        [JsonProperty]
        public string[] platforms;

        /// <summary>
        /// Description of the diagnostic
        /// </summary>
        [JsonRequired]
        public string description;

        /// <summary>
        /// Recommendation to fix the diagnostic
        /// </summary>
        [JsonRequired]
        public string solution;

        /// <summary>
        /// Url to documentation
        /// </summary>
        [JsonIgnore]
        public string documentationUrl;

        [JsonProperty("documentationUrl")]
        internal string documentationUrlForJson
        {
            get => string.IsNullOrEmpty(documentationUrl) ? null : documentationUrl;
            set => documentationUrl = string.IsNullOrEmpty(value) ? String.Empty : value;
        }

        /// <summary>
        /// Minimum Unity version this diagnostic applies to. If not specified, the diagnostic applies to all versions
        /// </summary>
        [JsonIgnore]
        public string minimumVersion;

        [JsonProperty("minimumVersion")]
        internal string minimumVersionForJson
        {
            get => string.IsNullOrEmpty(minimumVersion) ? null : minimumVersion;
            set => minimumVersion = string.IsNullOrEmpty(value) ? String.Empty : value;
        }


        /// <summary>
        /// Maximum Unity version this diagnostic applies to. If not specified, the diagnostic applies to all versions
        /// </summary>
        [JsonIgnore]
        public string maximumVersion;

        [JsonProperty("maximumVersion")]
        internal string maximumVersionForJson
        {
            get => string.IsNullOrEmpty(maximumVersion) ? null : maximumVersion;
            set => maximumVersion = string.IsNullOrEmpty(value) ? String.Empty : value;
        }


        /// <summary>
        /// Optional Auto-fixer
        /// </summary>
        [JsonIgnore]
        public Action<ProjectIssue> fixer;

        /// <summary>
        /// Name of the type (namespace and class/struct) of a known code API issue
        /// </summary>
        [JsonIgnore]
        public string type;

        [JsonProperty("type")]
        internal string typeForJson
        {
            get => string.IsNullOrEmpty(type) ? null : type;
            set => type = string.IsNullOrEmpty(value) ? String.Empty : value;
        }

        /// <summary>
        /// Name of the method of a known code API issue
        /// </summary>
        [JsonIgnore]
        public string method;

        [JsonProperty("method")]
        internal string methodForJson
        {
            get => string.IsNullOrEmpty(method) ? null : method;
            set => method = string.IsNullOrEmpty(value) ? String.Empty : value;
        }

        /// <summary>
        /// The evaluated value of a know code API issue
        /// </summary>
        [JsonIgnore]
        public string value;

        [JsonProperty("value")]
        internal string valueForJson
        {
            get => string.IsNullOrEmpty(value) ? null : value;
            set => this.value = string.IsNullOrEmpty(value) ? String.Empty : value;
        }

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
    }
}
