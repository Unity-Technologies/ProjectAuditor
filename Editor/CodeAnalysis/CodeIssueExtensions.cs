using System;

namespace Unity.ProjectAuditor.Editor.CodeAnalysis
{
    public static class CodeIssueExtensions
    {
        public static string GetCallingMethod(this ProjectIssue issue)
        {
            if (issue.dependencies == null)
                return string.Empty;

            var callTree = (CallTreeNode)issue.dependencies;
            return callTree.name;
        }
    }
}
