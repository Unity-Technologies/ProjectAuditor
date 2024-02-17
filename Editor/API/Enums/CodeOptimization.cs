using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Unity.ProjectAuditor.Editor
{
    /// <summary>
    /// Options for selecting the code optimization level to be used during code analysis.
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum CodeOptimization
    {
        /// <summary>
        /// Debug code optimization
        /// </summary>
        Debug,

        /// <summary>
        /// Release code optimization
        /// </summary>
        Release
    }
}
