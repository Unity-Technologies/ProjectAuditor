using System;
using Unity.ProjectAuditor.Editor.CodeAnalysis;
using Unity.ProjectAuditor.Editor.Utils;

namespace Unity.ProjectAuditor.Editor
{
    /// <summary>
    /// Affected area
    /// </summary>
    public enum Area
    {
        /// <summary>
        /// CPU Performance
        /// </summary>
        CPU,

        /// <summary>
        /// GPU Performance
        /// </summary>
        GPU,

        /// <summary>
        /// Memory consumption
        /// </summary>
        Memory,

        /// <summary>
        /// Application size
        /// </summary>
        BuildSize,

        /// <summary>
        /// Load times
        /// </summary>
        LoadTimes,

        /// <summary>
        /// All areas
        /// </summary>
        All
    }

    public enum IssueCategory
    {
        ApiCalls,
        ProjectSettings,
        NumCategories
    }

    /// <summary>
    /// ProjectAuditor Issue found in the current project
    /// </summary>
    [Serializable]
    public class ProjectIssue
    {
        public string assembly;
        public CallTreeNode callTree;
        public IssueCategory category;

        public string description;
        public ProblemDescriptor descriptor;
        public Location location;

        /// <summary>
        /// ProjectIssue constructor
        /// </summary>
        /// <param name="descriptor"> descriptor </param>
        /// <param name="description"> Issue-specific description of the problem </param>
        /// <param name="category"> Issue category </param>
        /// <param name="location"> Issue address </param>
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
                return location == null ? string.Empty : location.Filename;
            }
        }

        public string relativePath
        {
            get
            {
                return location == null ? string.Empty : location.Path;
            }
        }

        public int line
        {
            get
            {
                return location == null ? 0 : location.Line;
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
