using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Unity.ProjectAuditor.Editor
{
    /// <summary>
    /// Severity of an issue
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum Severity
    {
        /// <summary>
        /// Default Severity
        /// </summary>
        Default = 0,

        /// <summary>
        /// An error that will prevent a successful build - for example, a code compile error encountered during code analysis
        /// </summary>
        Error = 1,

        /// <summary>
        /// Critical impact on performance, quality or functionality
        /// </summary>
        Critical,

        /// <summary>
        /// Significant impact
        /// </summary>
        Major,

        /// <summary>
        /// Moderate impact
        /// </summary>
        Moderate,

        /// <summary>
        /// Minor impact
        /// </summary>
        Minor,

        /// <summary>
        /// A compiler warning encountered during code analysis
        /// </summary>
        Warning = Moderate,

        /// <summary>
        /// Something which is reported for informational purposes only, not necessarily a problem
        /// </summary>
        Info = Minor,

        /// <summary>
        /// Suppressed, ignored by UI and build
        /// </summary>
        None,

        /// <summary>
        /// Not visible to user
        /// </summary>
        Hidden,
    }
}
