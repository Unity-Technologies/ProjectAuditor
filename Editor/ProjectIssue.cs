using System;
using Unity.ProjectAuditor.Editor.CodeAnalysis;
using Unity.ProjectAuditor.Editor.Utils;
using UnityEngine;

namespace Unity.ProjectAuditor.Editor
{
    public enum IssueCategory
    {
        MetaData,
        Assets,
        Shaders,
        ShaderVariants,
        Code,
        Generics,
        ProjectSettings,
        BuildFiles,

        NumCategories
    }

    /// <summary>
    /// ProjectAuditor Issue found in the current project
    /// </summary>
    [Serializable]
    public class ProjectIssue
    {
        public DependencyNode dependencies;
        public IssueCategory category;

        public string description;
        public ProblemDescriptor descriptor;
        public Location location;

        [SerializeField] string[] customProperties;

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
            string[] customProperties)
            : this(descriptor, description, category)
        {
            this.customProperties = customProperties;
        }

        public ProjectIssue(ProblemDescriptor descriptor,
                            string description,
                            IssueCategory category,
                            string[] customProperties)
            : this(descriptor, description, category)
        {
            this.customProperties = customProperties;
        }

        public ProjectIssue(ProblemDescriptor descriptor,
                            string description,
                            IssueCategory category,
                            CallTreeNode dependenciesNode)
            : this(descriptor, description, category)
        {
            dependencies = dependenciesNode;
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

        public bool isPerfCriticalContext
        {
            get
            {
                return descriptor.critical || (dependencies != null && dependencies.IsPerfCritical());
            }
        }

        public Rule.Severity severity
        {
            get
            {
                return descriptor.severity;
            }
        }

        public string name
        {
            get
            {
                if (dependencies == null)
                    return string.Empty;
                var prettyName = dependencies.prettyName;
                if (prettyName.Equals(descriptor.description))
                    // if name matches the descriptor's name, use caller's name instead
                    return string.IsNullOrEmpty(this.GetCallingMethod()) ? string.Empty : dependencies.GetChild().prettyName;
                return prettyName;
            }
        }

        public int GetNumCustomProperties()
        {
            return customProperties != null ? customProperties.Length : 0;
        }

        public string GetCustomProperty(int index)
        {
            return customProperties != null ? customProperties[index] : string.Empty;
        }

        internal bool GetCustomPropertyAsBool(int index)
        {
            var valueAsString = GetCustomProperty(index);
            var value = false;
            if (!bool.TryParse(valueAsString, out value))
                return false;
            return value;
        }

        internal int GetCustomPropertyAsInt(int index)
        {
            var valueAsString = GetCustomProperty(index);
            var value = 0;
            if (!int.TryParse(valueAsString, out value))
                return 0;
            return value;
        }

        public void SetCustomProperty(int index, string property)
        {
            customProperties[index] = property;
        }

        public void SetCustomProperties(string[] properties)
        {
            customProperties = properties;
        }
    }
}
