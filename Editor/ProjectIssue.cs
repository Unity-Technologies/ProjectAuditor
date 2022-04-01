using System;
using System.Linq;
using Unity.ProjectAuditor.Editor.Utils;
using UnityEngine;

namespace Unity.ProjectAuditor.Editor
{
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

        [SerializeField] string[] m_CustomProperties;
        [SerializeField] Rule.Severity m_Severity;

        /// <summary>
        /// ProjectIssue constructor
        /// </summary>
        /// <param name="descriptor"> descriptor </param>
        /// <param name="description"> human-readable description </param>
        /// <param name="category"> Issue category </param>
        /// <param name="location"> Issue address: path and, if applicable, line number </param>
        /// <param name="customProperties"> Issue-specific properties </param>
        public ProjectIssue(ProblemDescriptor descriptor,
                            string description,
                            IssueCategory category,
                            Location location = null,
                            object[] customProperties = null)
        {
            this.descriptor = descriptor;
            this.description = description;
            this.category = category;
            this.location = location;
            this.SetCustomProperties(customProperties);

            m_Severity = Rule.Severity.Default;
        }

        public ProjectIssue(ProblemDescriptor descriptor,
                            string description,
                            IssueCategory category,
                            string path,
                            object[] customProperties)
            : this(descriptor, description, category, new Location(path), customProperties)
        {
        }

        public ProjectIssue(ProblemDescriptor descriptor,
                            string description,
                            IssueCategory category,
                            object[] customProperties)
            : this(descriptor, description, category)
        {
            if (customProperties != null)
                this.m_CustomProperties = customProperties.Select(p => p.ToString()).ToArray();
        }

        public int depth = 0;

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
                return m_Severity == Rule.Severity.Default && descriptor != null ? descriptor.severity : m_Severity;
            }
            set
            {
                m_Severity = value;
            }
        }

        public int GetNumCustomProperties()
        {
            return m_CustomProperties != null ? m_CustomProperties.Length : 0;
        }

        public string GetCustomProperty<T>(T propertyEnum) where T : struct
        {
            return GetCustomProperty(Convert.ToInt32(propertyEnum));
        }

        internal string GetCustomProperty(int index)
        {
            return m_CustomProperties != null && m_CustomProperties.Length > 0 ? m_CustomProperties[index] : string.Empty;
        }

        internal bool GetCustomPropertyAsBool<T>(T propertyEnum) where T : struct
        {
            var valueAsString = GetCustomProperty(propertyEnum);
            var value = false;
            if (!bool.TryParse(valueAsString, out value))
                return false;
            return value;
        }

        internal int GetCustomPropertyAsInt<T>(T propertyEnum) where T : struct
        {
            var valueAsString = GetCustomProperty(propertyEnum);
            var value = 0;
            if (!int.TryParse(valueAsString, out value))
                return 0;
            return value;
        }

        internal long GetCustomPropertyAsLong<T>(T propertyEnum) where T : struct
        {
            var valueAsString = GetCustomProperty(propertyEnum);
            var value = (long)0;
            if (!long.TryParse(valueAsString, out value))
                return 0;
            return value;
        }

        public float GetCustomPropertyAsFloat<T>(T propertyEnum) where T : struct
        {
            float value;
            return float.TryParse(GetCustomProperty((propertyEnum)), out value) ? value : float.NaN;
        }

        public double GetCustomPropertyAsDouble<T>(T propertyEnum) where T : struct
        {
            double value;
            return double.TryParse(GetCustomProperty((propertyEnum)), out value) ? value : double.NaN;
        }

        public void SetCustomProperty<T>(T propertyEnum, object property) where T : struct
        {
            m_CustomProperties[Convert.ToUInt32(propertyEnum)] = property.ToString();
        }

        /// <summary>
        /// Initialize all custom properties to the same value
        /// </summary>
        /// <param name="numProperties"> total number of custom properties </param>
        /// <param name="property"> value the properties will be set to </param>
        public void SetCustomProperties(int numProperties, object property)
        {
            m_CustomProperties = new string[numProperties];
            for (var i = 0; i < numProperties; i++)
                m_CustomProperties[i] = property.ToString();
        }

        public void SetCustomProperties(object[] properties)
        {
            if (properties != null)
                this.m_CustomProperties = properties.Select(p => p != null ? p.ToString() : string.Empty).ToArray();
            else
                this.m_CustomProperties = null;
        }
    }
}
