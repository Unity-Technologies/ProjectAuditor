using System;
using Unity.ProjectAuditor.Editor.CodeAnalysis;
using UnityEngine;

namespace Unity.ProjectAuditor.Editor
{
    public class TextFilter : IProjectIssueFilter
    {
        public bool ignoreCase = true;
        public bool searchDependencies = false;
        public string searchText = string.Empty;

        public bool Match(ProjectIssue issue)
        {
            if (string.IsNullOrEmpty(searchText))
                return true;

            // return true if the issue matches the any of the following string search criteria
            if (MatchesSearch(issue.description))
                return true;

            if (MatchesSearch(issue.filename))
                return true;

            var dependencies = issue.dependencies;
            if (dependencies != null)
            {
                if (MatchesSearch(dependencies, searchDependencies))
                    return true;
            }

            // no string match
            return false;
        }

        bool MatchesSearch(string text)
        {
            return !string.IsNullOrEmpty(text) &&
                text.IndexOf(searchText, ignoreCase ? StringComparison.CurrentCultureIgnoreCase : StringComparison.CurrentCulture) >= 0;
        }

        bool MatchesSearch(DependencyNode node, bool recursive)
        {
            if (node == null)
                return false;

            var callTreeNode = node as CallTreeNode;
            if (callTreeNode != null)
            {
                if (MatchesSearch(callTreeNode.typeName) || MatchesSearch(callTreeNode.methodName))
                    return true;
            }
            if (recursive)
                for (var i = 0; i < node.GetNumChildren(); i++)
                {
                    if (MatchesSearch(node.GetChild(i), true))
                        return true;
                }

            return false;
        }
    }
}
