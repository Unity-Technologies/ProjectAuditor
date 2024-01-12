namespace Unity.ProjectAuditor.Editor.Interfaces
{
    internal interface IIssueFilter
    {
        bool Match(ReportItem issue);
    }
}
