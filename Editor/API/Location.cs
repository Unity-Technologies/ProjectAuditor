using System;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;

namespace Unity.ProjectAuditor.Editor
{
    /// <summary>
    /// Represents the location of a reported issue.
    /// </summary>
    [Serializable]
    public class Location : ISerializationCallbackReceiver
    {
        [SerializeField] int m_Line;
        [SerializeField] string m_Path; // path relative to the project folder
        Func<string> m_PathGenerator;

        /// <summary>
        /// File extension
        /// </summary>
        [JsonIgnore]
        public string Extension
        {
            get
            {
                if (m_Extension == null)
                {
                    m_Extension = System.IO.Path.GetExtension(Path) ?? string.Empty;
                    if (m_Extension.StartsWith("."))
                        m_Extension = m_Extension.Substring(1);
                }

                return m_Extension;
            }
        }

        /// <summary>
        /// Filename
        /// </summary>
        [JsonIgnore]
        public string Filename
        {
            get
            {
                if (m_Filename == null)
                    m_Filename = string.IsNullOrEmpty(Path) ? string.Empty : System.IO.Path.GetFileName(Path);
                return m_Filename;
            }
        }

        /// <summary>
        /// Formatted filename with line number
        /// </summary>
        [JsonIgnore]
        public string FormattedFilename
        {
            get
            {
                if (m_FormattedFilename == null)
                    m_FormattedFilename = GetFormattedPath(Filename);
                return m_FormattedFilename;
            }
        }

        /// <summary>
        /// Formatted path with line number
        /// </summary>
        [JsonIgnore]
        public string FormattedPath
        {
            get
            {
                if (m_FormattedPath == null)
                    m_FormattedPath = GetFormattedPath(Path);
                return m_FormattedPath;
            }
        }

        /// <summary>
        /// Checks whether the location is valid
        /// </summary>
        /// <value>True if the location is valid</value>
        public bool IsValid => !string.IsNullOrEmpty(Path);

        /// <summary>
        /// Line number
        /// </summary>
        [JsonProperty("line", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public int Line => m_Line;

        /// <summary>
        /// Full path
        /// </summary>
        [JsonIgnore]
        public string Path
        {
            get
            {
                if (m_Path == null && m_PathGenerator != null)
                {
                    m_Path = m_PathGenerator.Invoke().Replace($"{ProjectAuditor.ProjectPath}/", string.Empty);
                }
                m_PathGenerator = null;
                return m_Path ?? string.Empty;
            }
        }

        [JsonProperty("path")]
        internal string PathForJson
        {
            get
            {
                if (string.IsNullOrEmpty(Path))
                    return null;
                return Path.Replace(EditorApplication.applicationContentsPath,
                    "UNITY_PATH/Data");
            }
            set
            {
                if (string.IsNullOrEmpty(value))
                    m_Path = string.Empty;
                else
                    m_Path = value.Replace("UNITY_PATH/Data", EditorApplication.applicationContentsPath);
            }
        }

        // Cached string storage to reduce constant string manipulation in UI
        string m_Extension;
        string m_Filename;
        string m_FormattedFilename;
        string m_FormattedPath;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="path">File path</param>
        public Location(string path)
        {
            m_Path = path.Replace($"{ProjectAuditor.ProjectPath}/", string.Empty);
        }

        /// <summary>
        /// Constructor with line number
        /// </summary>
        /// <param name="path">File path</param>
        /// <param name="line">Line number</param>
        [JsonConstructor]
        public Location(string path, int line)
        {
            if (path != null)
                m_Path = path.Replace($"{ProjectAuditor.ProjectPath}/", string.Empty);
            m_Line = line;
        }

        internal Location(Func<string> pathGenerator, int line)
        {
            if (pathGenerator != null)
                m_PathGenerator = pathGenerator;
            m_Line = line;
        }

        string GetFormattedPath(string path)
        {
            if (path.EndsWith(".cs"))
                return $"{path}:{m_Line}";
            return path;
        }

        public void OnBeforeSerialize()
        {
            if (m_Path == null && m_PathGenerator != null)
            {
                m_Path = m_PathGenerator.Invoke().Replace($"{ProjectAuditor.ProjectPath}/", string.Empty);
            }
            m_PathGenerator = null;
        }

        public void OnAfterDeserialize()
        {
        }
    }
}
