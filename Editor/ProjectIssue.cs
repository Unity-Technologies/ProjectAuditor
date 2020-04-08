using System;
using Unity.ProjectAuditor.Editor.CodeAnalysis;
using Unity.ProjectAuditor.Editor.Utils;

namespace Unity.ProjectAuditor.Editor
{
    public enum Area
    {
        CPU,
        GPU,
        Memory,
        BuildSize,
        LoadTimes,

        All
    }

    public enum IssueCategory
    {
        ApiCalls,
        ProjectSettings,
        NumCategories
    }

    [Serializable]
    public class ProjectIssue
    {
        public string assembly;
        public CallTreeNode callTree;
        public IssueCategory category;

        public string description;
        public ProblemDescriptor descriptor;
        public Location location;

        private ProjectIssue()
        {
        }

        public ProjectIssue(ProblemDescriptor descriptor,
                            string description,
                            IssueCategory category,
                            Location location = null)
        {
            this.descriptor = descriptor;
            this.description = description;
            this.category = category;
            this.location = location;
        }

        public ProjectIssue(ProblemDescriptor descriptor,
                            string description,
                            IssueCategory category,
                            CallTreeNode callTreeNode)
        {
            this.descriptor = descriptor;
            this.description = description;
            this.category = category;
            callTree = callTreeNode;
        }

        public string filename
        {
            get
            {
                return location == null ? string.Empty : location.filename;
            }
        }

        public string relativePath
        {
            get
            {
                return location == null ? string.Empty : location.relativePath;
            }
        }

        public int line
        {
            get
            {
                return location == null ? 0 : location.line;
            }
        }

        public string callingMethod
        {
            get
            {
                if (callTree == null)
                    return string.Empty;
                if (!callTree.HasChildren())
                    return string.Empty;

                return callTree.GetChild().name;
            }
        }

        public bool isPerfCriticalContext
        {
            get
            {
                return callTree != null && callTree.IsPerfCriticalContext();
            }
        }

        public string name
        {
            get
            {
                if (callTree == null)
                    return string.Empty;
                if (callTree.prettyName.Equals(descriptor.description))
                    // if name matches the descriptor's name, use caller's name instead
                    return string.IsNullOrEmpty(callingMethod) ? string.Empty : callTree.GetChild().prettyName;
                return callTree.prettyName;
            }
        }
    }
}
