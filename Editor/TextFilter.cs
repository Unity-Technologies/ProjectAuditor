using System;
using Unity.ProjectAuditor.Editor.CodeAnalysis;
using UnityEngine;

namespace Unity.ProjectAuditor.Editor
{
    [Serializable]
    public class TextFilter : IProjectIssueFilter
    {
        public bool matchCase = false;
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

        private bool MatchesSearch(string text)
        {
            return !string.IsNullOrEmpty(text) &&
                text.IndexOf(searchText, matchCase ? StringComparison.CurrentCulture : StringComparison.CurrentCultureIgnoreCase) >= 0;
        }

        private bool MatchesSearch(DependencyNode node, bool recursive)
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
                for (int i = 0; i < callTreeNode.GetNumChildren(); i++)
                {
                    if (MatchesSearch(callTreeNode.GetChild(i), true))
                        return true;
                }

            return false;
        }
    }
}
