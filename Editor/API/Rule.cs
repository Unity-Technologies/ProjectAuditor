using System;
using Newtonsoft.Json;

namespace Unity.ProjectAuditor.Editor
{
    /// <summary>
    /// Represents a rule which modifies the <seealso cref="Severity"/> of an Issue <seealso cref="ReportItem"/>
    /// or all of the ProjectIssues that share a <seealso cref="Descriptor"/>.
    /// </summary>
    [Serializable]
    public class Rule
    {
        /// <summary>
        /// The Severity level to apply to the issue(s) represented by this Rule
        /// </summary>
        [JsonProperty("severity")]
        public Severity Severity;

        /// <summary>
        /// An optional location filter representing a ReportItem's location.
        /// If specified, this Rule applies to a single ReportItem. If the string is null or empty, this Rule applies to every ReportItem matching the Id.
        /// </summary>
        [JsonProperty("filter")]
        public string Filter;

        /// <summary>
        /// The Descriptor ID
        /// </summary>
        [JsonIgnore]
        public DescriptorId Id;

        [JsonProperty("id")]
        private string idAsString
        {
            get => Id.AsString();
            set => Id = value;
        }

        /// <summary>Get the hashed integer representation of the Rule.</summary>
        /// <returns>The computed hash code.</returns>
        public override int GetHashCode()
        {
            unchecked
            {
                var hash = 17;
                hash = hash * 23 + Id.GetHashCode();
                hash = hash * 23 + Filter.GetHashCode();
                hash = hash * 23 + Severity.GetHashCode();
                return hash;
            }
        }
    }
}
