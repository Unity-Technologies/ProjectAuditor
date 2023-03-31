using System;

namespace Unity.ProjectAuditor.Editor
{
    internal interface IProjectIssueFilter
    {
        bool Match(ProjectIssue issue);
    }
}
