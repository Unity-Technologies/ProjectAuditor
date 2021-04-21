using System;

namespace Unity.ProjectAuditor.Editor.CodeAnalysis
{
    public static class CodeIssueExtensions
    {
        public static string GetCallingMethod(this ProjectIssue issue)
        {
            if (issue.dependencies == null)
                return string.Empty;
            if (!issue.dependencies.HasChildren())
                return string.Empty;

            var callTree = issue.dependencies.GetChild() as CallTreeNode;
            if (callTree == null)
                return string.Empty;
            return callTree.name;
        }
    }
}
