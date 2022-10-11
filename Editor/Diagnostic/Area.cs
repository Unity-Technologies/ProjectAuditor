namespace Unity.ProjectAuditor.Editor.Diagnostic
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
}
