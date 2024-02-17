using System;
using Newtonsoft.Json;
using Unity.ProjectAuditor.Editor.Core;
using UnityEngine;

namespace Unity.ProjectAuditor.Editor
{
    /// <summary>
    /// Describes an issue that ProjectAuditor reports in the Unity project.
    /// </summary>
    [Serializable]
    public class ReportItem
    {
        [SerializeField][JsonIgnore]
        DescriptorId m_DescriptorId;

        [SerializeField]
        IssueCategory m_Category;

        [SerializeField]
        string m_Description;

        [SerializeField]
        Severity m_Severity;

        [SerializeField]
        DependencyNode m_Dependencies;

        [SerializeField]
        Location m_Location;

        [SerializeField]
        string[] m_CustomProperties;

        /// <summary>
        /// Determines whether the Issue was fixed. Only used if the ReportItem represents an Issue.
        /// </summary>
        [JsonIgnore]
        [SerializeField]
        internal bool WasFixed = false;

        /// <summary>
        /// An unique identifier for the issue descriptor (read-only).
        /// </summary>
        /// <remarks>
        /// Reports can contain two different types of ReportItem:
        /// - Issues, which indicate a potential problem which should be investigated and possibly fixed: for example, a texture with its Read/Write Enabled checkbox ticked.
        /// - Insights, for informational purposes: for example, general information about a texture in the project.
        ///
        /// Issues can be identified by having a valid <seealso cref="DescriptorId"/>. See also: the <seealso cref="IsIssue"/> method.
        /// </remarks>
        [JsonIgnore]
        public DescriptorId Id
        {
            get => m_DescriptorId;
            internal set => m_DescriptorId = value;
        }

        [JsonProperty("descriptorId")]
        internal string DescriptorIdAsString
        {
            get { return m_DescriptorId.IsValid() ? m_DescriptorId.AsString() : null; }
            set
            {
                // TODO: check if ID is registered
                m_DescriptorId = new DescriptorId(value);
            }
        }

        /// <summary>
        /// This issue's category (read-only).
        /// </summary>
        [JsonProperty("category")]
        public IssueCategory Category
        {
            get => m_Category;
            internal set => m_Category = value;
        }

        /// <summary>
        /// Custom properties.
        /// See the "moduleMetadata" section of an exported Report JSON file for information on the formats and
        /// meanings of the custom properties for each IssueCategory.
        /// </summary>
        [JsonProperty("properties")]
        public string[] CustomProperties
        {
            get => m_CustomProperties;
            internal set => m_CustomProperties = value;
        }

        /// <summary>
        /// Project issue Description (read-only).
        /// </summary>
        [JsonProperty("description")]
        public string Description
        {
            get => m_Description;
            internal set => m_Description = value;
        }

        /// <summary>
        /// Dependencies of this project issue.
        /// </summary>
        internal DependencyNode Dependencies
        {
            get => m_Dependencies;
            set => m_Dependencies = value;
        }

        /// <summary>
        /// Name of the file that contains this issue.
        /// </summary>
        [JsonIgnore]
        public string Filename
        {
            get { return m_Location == null ? string.Empty : m_Location.Filename; }
        }

        /// <summary>
        /// Relative path of the file that contains this issue.
        /// </summary>
        [JsonIgnore]
        public string RelativePath
        {
            get { return m_Location == null ? string.Empty : m_Location.Path; }
        }

        /// <summary>
        /// Line in the file that contains this issue.
        /// </summary>
        [JsonIgnore]
        public int Line
        {
            get { return m_Location == null ? 0 : m_Location.Line; }
        }

        /// <summary>
        /// Location of the Insight or Issue (read-only).
        /// </summary>
        [JsonProperty("location")]
        public Location Location
        {
            get => m_Location;
            internal set => m_Location = value;
        }

        /// <summary>
        /// Log level.
        /// </summary>
        [JsonIgnore]
        public LogLevel LogLevel
        {
            get
            {
                switch (Severity)
                {
                    case Severity.Error:
                        return LogLevel.Error;
                    case Severity.Warning:
                        return LogLevel.Warning;
                    case Severity.Info:
                    default:
                        return LogLevel.Info;
                }
            }
        }

        /// <summary>
        /// Issue-specific Severity (read-only).
        /// </summary>
        [JsonIgnore]
        public Severity Severity
        {
            get => m_Severity == Severity.Default && m_DescriptorId.IsValid() ? m_DescriptorId.GetDescriptor().DefaultSeverity : m_Severity;
            internal set => m_Severity = value;
        }

        [JsonProperty("severity")]
        internal string SeverityAsString
        {
            get => IsIssue() ? m_Severity.ToString() : null;
            set => m_Severity = (Severity)Enum.Parse(typeof(Severity), value);
        }

        [JsonConstructor]
        internal ReportItem()
        {
            // only for json serialization purposes
            m_DescriptorId = new DescriptorId(string.Empty);
        }

        /// <summary>
        /// Constructs and returns an instance of ReportItem.
        /// </summary>
        /// <param name="category">Issue category</param>
        /// <param name="id">Descriptor ID</param>
        /// <param name="args">Arguments to be used in the message formatting</param>
        internal ReportItem(IssueCategory category, string id, params object[] args)
        {
            m_DescriptorId = new DescriptorId(id);
            var descriptor = DescriptorLibrary.GetDescriptor(m_DescriptorId.AsInt());

            m_Category = category;

            try
            {
                m_Description = string.IsNullOrEmpty(descriptor.MessageFormat) ? descriptor.Title : string.Format(descriptor.MessageFormat, args);
            }
            catch (Exception e)
            {
                Debug.LogError("Error formatting message: " + descriptor.MessageFormat + " with args: " + string.Join(", ", args) + " - " + e.Message);
                m_Description = descriptor.Title;
            }
            m_Severity = descriptor.DefaultSeverity;
        }

        /// <summary>
        /// Constructs and returns an instance of ReportItem.
        /// </summary>
        /// <param name="category">Issue category</param>
        /// <param name="description">Issue description</param>
        internal ReportItem(IssueCategory category, string description)
        {
            m_DescriptorId = new DescriptorId(null);  // Empty, invalid descriptor
            m_Category = category;
            m_Description = description;
            m_Severity = Severity.Default;
        }

        /// <summary>
        /// Checks whether this ReportItem represents an Issue.
        /// </summary>
        /// <returns>True if the issue's descriptor ID is valid. Otherwise, returns false.</returns>
        public bool IsIssue()
        {
            return Id.IsValid();
        }

        /// <summary>
        /// Checks whether this issue is major or critical.
        /// </summary>
        /// <returns>True if the issue's Severity is Major or Critical. Otherwise, returns false.</returns>
        public bool IsMajorOrCritical()
        {
            return Severity == Severity.Critical || Severity == Severity.Major;
        }

        /// <summary>
        /// Checks whether this issue is valid.
        /// </summary>
        /// <returns>True if the issue has a valid description string. Otherwise, returns false.</returns>
        public bool IsValid()
        {
            return Description != null;
        }

        /// <summary>
        /// Gets the number of custom properties this issue has.
        /// </summary>
        /// <returns>The number of custom property strings</returns>
        public int GetNumCustomProperties()
        {
            return m_CustomProperties != null ? m_CustomProperties.Length : 0;
        }

        // stephenm TODO: The Get/SetCustomProperty methods need more explanation - like how do you find out what enum is used for a given ReportItem.
        /// <summary>
        /// Get a custom property string given an enum.
        /// </summary>
        /// <param name="propertyEnum">Enum value indicating a property.</param>
        /// <typeparam name="T">Can be any struct, but the method expects an enum</typeparam>
        /// <returns>Property name string</returns>
        public string GetCustomProperty<T>(T propertyEnum) where T : struct
        {
            return GetCustomProperty(Convert.ToInt32(propertyEnum));
        }

        /// <summary>
        /// Get a custom property string given an index into the custom properties array.
        /// </summary>
        /// <param name="index">Custom property index</param>
        /// <returns>Property name string. Returns empty string if the custom properties array is null or empty or if the index is out of range.</returns>
        internal string GetCustomProperty(int index)
        {
            if (m_CustomProperties == null ||
                m_CustomProperties.Length == 0 ||
                index < 0 ||
                index >= m_CustomProperties.Length)
                return string.Empty; // fail gracefully if layout changed
            return m_CustomProperties[index];
        }

        /// <summary>
        /// Check whether a custom property is a boolean type and whether its value is true.
        /// </summary>
        /// <param name="propertyEnum">Enum value indicating a property.</param>
        /// <typeparam name="T">Can be any struct, but the method expects an enum</typeparam>
        /// <returns>Returns the property's value if the property is valid and if the value type is boolean. Otherwise, returns false.</returns>
        public bool GetCustomPropertyBool<T>(T propertyEnum) where T : struct
        {
            var valueAsString = GetCustomProperty(propertyEnum);
            var value = false;
            if (!bool.TryParse(valueAsString, out value))
                return false;
            return value;
        }

        /// <summary>
        /// Check whether a custom property is an integer type and return its value.
        /// </summary>
        /// <param name="propertyEnum">Enum value indicating a property.</param>
        /// <typeparam name="T">Can be any struct, but the method expects an enum</typeparam>
        /// <returns>Returns the property's value if the property is valid and if the value is an integer type. Otherwise, returns 0.</returns>
        public int GetCustomPropertyInt32<T>(T propertyEnum) where T : struct
        {
            var valueAsString = GetCustomProperty(propertyEnum);
            var value = 0;
            if (!int.TryParse(valueAsString, out value))
                return 0;
            return value;
        }

        /// <summary>
        /// Check whether a custom property is a long type and return its value.
        /// </summary>
        /// <param name="propertyEnum">Enum value indicating a property.</param>
        /// <typeparam name="T">Can be any struct, but the method expects an enum</typeparam>
        /// <returns>Returns the property's value if the property is valid and if the value is a long type. Otherwise, returns 0.</returns>
        public long GetCustomPropertyInt64<T>(T propertyEnum) where T : struct
        {
            var valueAsString = GetCustomProperty(propertyEnum);
            var value = (long)0;
            if (!long.TryParse(valueAsString, out value))
                return 0;
            return value;
        }

        /// <summary>
        /// Check whether a custom property is a ulong type and return its value.
        /// </summary>
        /// <param name="propertyEnum">Enum value indicating a property.</param>
        /// <typeparam name="T">Can be any struct, but the method expects an enum</typeparam>
        /// <returns>Returns the property's value if the property is valid and if the value is a ulong type. Otherwise, returns 0.</returns>
        public ulong GetCustomPropertyUInt64<T>(T propertyEnum) where T : struct
        {
            var valueAsString = GetCustomProperty(propertyEnum);
            var value = (ulong)0;
            if (!ulong.TryParse(valueAsString, out value))
                return 0;
            return value;
        }

        /// <summary>
        /// Check whether a custom property is a float type and return its value.
        /// </summary>
        /// <param name="propertyEnum">Enum value indicating a property.</param>
        /// <typeparam name="T">Can be any struct, but the method expects an enum</typeparam>
        /// <returns>Returns the property's value if the property is valid and if the value is a float type. Otherwise, returns 0.0f.</returns>
        public float GetCustomPropertyFloat<T>(T propertyEnum) where T : struct
        {
            float value;
            return float.TryParse(GetCustomProperty(propertyEnum), out value) ? value : 0.0f;
        }

        /// <summary>
        /// Check whether a custom property is a double type and return its value.
        /// </summary>
        /// <param name="propertyEnum">Enum value indicating a property.</param>
        /// <typeparam name="T">Can be any struct, but the method expects an enum</typeparam>
        /// <returns>Returns the property's value if the property is valid and if the value is a double type. Otherwise, returns 0.0.</returns>
        public double GetCustomPropertyDouble<T>(T propertyEnum) where T : struct
        {
            double value;
            return double.TryParse(GetCustomProperty(propertyEnum), out value) ? value : 0.0;
        }

        /// <summary>
        /// Set a custom property.
        /// </summary>
        /// /// <param name="propertyEnum">Enum value indicating a property.</param>
        /// <typeparam name="T">Can be any struct, but the method expects an enum</typeparam>
        /// <param name="property">An object containing a value for the property</param>
        public void SetCustomProperty<T>(T propertyEnum, object property) where T : struct
        {
            m_CustomProperties[Convert.ToUInt32(propertyEnum)] = property.ToString();
        }
    }
}
