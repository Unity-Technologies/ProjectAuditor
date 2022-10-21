namespace Unity.ProjectAuditor.Editor.Diagnostic
{
    public enum Severity
    {
        Default, // default to TBD
        Error, // fails on build
        Warning, // logs a warning
        Info, // logs an info message
        None, // suppressed, ignored by UI and build
        Hidden // not visible to user
    }
}
