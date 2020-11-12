using System;

namespace Unity.ProjectAuditor.Editor
{
    public interface IProjectIssueFilter
    {
        bool Match(ProjectIssue issue);
    }
}
