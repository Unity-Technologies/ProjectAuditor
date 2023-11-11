using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Unity.ProjectAuditor.Editor
{
    // stephenm TODO: Elaborate on this. It's a bit minimal right now.
    /// <summary>
    /// Options for the compilation mode Project Auditor should use when performing code analysis
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum CompilationMode
    {
        /// <summary>
        ///   <para>Non-Development player (default)</para>
        /// </summary>
        Player,
        /// <summary>
        ///   <para>Development player</para>
        /// </summary>
        DevelopmentPlayer,

        /// <summary>
        ///   <para>Editor assemblies for Play Mode</para>
        /// </summary>
        EditorPlayMode,

        /// <summary>
        ///   <para>Editor assemblies</para>
        /// </summary>
        Editor
    }
}
