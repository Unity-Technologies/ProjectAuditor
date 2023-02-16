namespace Unity.ProjectAuditor.Editor.Diagnostic
{
    public enum Severity
    {
        Default = 0,

        Error = 1,

        Critical,   // Critical impact on performance, quality or functionality
        Major,      // Significant impact
        Moderate,   // Moderate impact
        Minor,      // Minor impact

        Warning,
        Info,

        None,       // suppressed, ignored by UI and build
        Hidden,     // not visible to user
    }
}
