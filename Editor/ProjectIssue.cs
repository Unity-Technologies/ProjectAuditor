using System;
using System.Linq;
using Unity.ProjectAuditor.Editor.Core;
using Unity.ProjectAuditor.Editor.Diagnostic;
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
        /// <summary>
        /// Create Diagnostics-specific IssueBuilder
        /// </summary>
        /// <param name="category"> Issue category </param>
        /// <param name="descriptor"> Diagnostic descriptor </param>
        /// <param name="args"> Arguments to be used in the message formatting</param>
        public static IssueBuilder Create(IssueCategory category, Descriptor descriptor, params object[] args)
        {
            return new IssueBuilder(category, descriptor, args);
        }

        /// <summary>
        /// Create General-purpose IssueBuilder
        /// </summary>
        /// <param name="category"> Issue category </param>
        /// <param name="description"> User-friendly description </param>
        public static IssueBuilder Create(IssueCategory category, string description)
        {
            return new IssueBuilder(category, description);
        }

        [SerializeField] IssueCategory m_Category;
        [SerializeField] string m_Description;
        [SerializeField] Descriptor m_Descriptor;

        [SerializeField] DependencyNode m_Dependencies;
        [SerializeField] Location m_Location;

        [SerializeField] string[] m_CustomProperties;
        [SerializeField] Severity m_Severity;
        [SerializeField] bool m_Critical;

        internal ProjectIssue(IssueCategory category, Descriptor descriptor, params object[] args)
        {
            m_Descriptor = descriptor;
            m_Description = args.Length > 0 ? string.Format(descriptor.messageFormat, args) : descriptor.title;
            m_Category = category;
            m_Critical = descriptor.critical;
            m_Severity = descriptor.severity;
        }

        internal ProjectIssue(IssueCategory category, string description)
        {
            m_Description = description;
            m_Category = category;
            m_Critical = false;
            m_Severity = Severity.Default;
        }

        public IssueCategory category => m_Category;

        public string[] customProperties
        {
            get => m_CustomProperties;
            internal set => m_CustomProperties = value;
        }

        public string description
        {
            get => m_Description;
            internal set => m_Description = value;
        }

        /// <summary>
        /// Optional descriptor. Only used for diagnostics
        /// </summary>
        public Descriptor descriptor => m_Descriptor;

        /// <summary>
        /// Determines whether the issue was fixed. Only used for diagnostics
        /// </summary>
        public bool wasFixed = false;

        public DependencyNode dependencies
        {
            get => m_Dependencies;
            internal set => m_Dependencies = value;
        }

        public int depth = 0;

        public string filename => m_Location == null ? string.Empty : m_Location.Filename;

        public string relativePath => m_Location == null ? string.Empty : m_Location.Path;

        public int line => m_Location == null ? 0 : m_Location.Line;

        /// <summary>
        /// Location of the item or diagnostic
        /// </summary>
        public Location location
        {
            get => m_Location;
            internal set => m_Location = value;
        }

        /// <summary>
        /// Diagnostics-specific severity
        /// </summary>
        public Severity severity
        {
            get => m_Severity == Severity.Default && descriptor != null ? descriptor.severity : m_Severity;
            set => m_Severity = value;
        }

        /// <summary>
        /// Diagnostics-specific priority
        /// </summary>
        public bool isCritical
        {
            get => m_Critical;
            set => m_Critical = value;
        }

        public bool IsValid()
        {
            return description != null;
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
            if (index >= m_CustomProperties.Length)
                return string.Empty; // fail gracefully if layout changed
            return m_CustomProperties != null && m_CustomProperties.Length > 0 ? m_CustomProperties[index] : string.Empty;
        }

        public bool GetCustomPropertyBool<T>(T propertyEnum) where T : struct
        {
            var valueAsString = GetCustomProperty(propertyEnum);
            var value = false;
            if (!bool.TryParse(valueAsString, out value))
                return false;
            return value;
        }

        public int GetCustomPropertyInt32<T>(T propertyEnum) where T : struct
        {
            var valueAsString = GetCustomProperty(propertyEnum);
            var value = 0;
            if (!int.TryParse(valueAsString, out value))
                return 0;
            return value;
        }

        public long GetCustomPropertyInt64<T>(T propertyEnum) where T : struct
        {
            var valueAsString = GetCustomProperty(propertyEnum);
            var value = (long)0;
            if (!long.TryParse(valueAsString, out value))
                return 0;
            return value;
        }

        public ulong GetCustomPropertyUInt64<T>(T propertyEnum) where T : struct
        {
            var valueAsString = GetCustomProperty(propertyEnum);
            var value = (ulong)0;
            if (!ulong.TryParse(valueAsString, out value))
                return 0;
            return value;
        }

        public float GetCustomPropertyFloat<T>(T propertyEnum) where T : struct
        {
            float value;
            return float.TryParse(GetCustomProperty(propertyEnum), out value) ? value : 0.0f;
        }

        public double GetCustomPropertyDouble<T>(T propertyEnum) where T : struct
        {
            double value;
            return double.TryParse(GetCustomProperty(propertyEnum), out value) ? value : 0.0;
        }

        public void SetCustomProperty<T>(T propertyEnum, object property) where T : struct
        {
            m_CustomProperties[Convert.ToUInt32(propertyEnum)] = property.ToString();
        }
    }
}
