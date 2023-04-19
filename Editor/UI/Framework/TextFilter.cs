using System;
using System.Collections.Generic;
using Unity.ProjectAuditor.Editor.CodeAnalysis;
using Unity.ProjectAuditor.Editor.Core;

namespace Unity.ProjectAuditor.Editor
{
    internal class TextFilter : IProjectIssueFilter
    {
        public bool ignoreCase = true;
        public bool searchDependencies = false;
        public string searchString = string.Empty;

        readonly int[] searchablePropertyIndices;

        public TextFilter(PropertyDefinition[] propertyDefinitions = null)
        {
            var indices = new List<int>();
            if (propertyDefinitions != null)
            {
                foreach (var propertyDefinition in propertyDefinitions)
                {
                    if (propertyDefinition.format != PropertyFormat.String)
                        continue;
                    if (!PropertyTypeUtil.IsCustom(propertyDefinition.type))
                        continue;
                    indices.Add(PropertyTypeUtil.ToCustomIndex(propertyDefinition.type));
                }
            }
            searchablePropertyIndices = indices.ToArray();
        }

        public bool Match(ProjectIssue issue)
        {
            if (string.IsNullOrEmpty(searchString))
                return true;

            // return true if the issue matches the any of the following string search criteria
            if (MatchesSearch(issue.description))
                return true;

            if (MatchesSearch(issue.filename))
                return true;

            foreach (var customPropertyIndex in searchablePropertyIndices)
            {
                if (MatchesSearch(issue.GetCustomProperty(customPropertyIndex)))
                    return true;
            }

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
                text.IndexOf(searchString, ignoreCase ? StringComparison.CurrentCultureIgnoreCase : StringComparison.CurrentCulture) >= 0;
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
