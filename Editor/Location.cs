using System;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;

namespace Unity.ProjectAuditor.Editor
{
    /// <summary>
    /// Location of a reported issue
    /// </summary>
    [Serializable]
    public class Location
    {
        [SerializeField] int m_Line;
        [SerializeField] string m_Path; // path relative to the project folder

        /// <summary>
        /// File extension
        /// </summary>
        [JsonIgnore]
        public string Extension => System.IO.Path.GetExtension(m_Path) ?? string.Empty;

        /// <summary>
        /// Filename
        /// </summary>
        [JsonIgnore]
        public string Filename => string.IsNullOrEmpty(m_Path) ? string.Empty : System.IO.Path.GetFileName(m_Path);

        /// <summary>
        /// Formatted filename with line number
        /// </summary>
        [JsonIgnore]
        public string FormattedFilename => GetFormattedPath(Filename);

        /// <summary>
        /// Formatted path with line number
        /// </summary>
        [JsonIgnore]
        public string FormattedPath => GetFormattedPath(Path);

        /// <summary>
        /// Line number
        /// </summary>
        [JsonProperty("line")]
        public int Line => m_Line;

        /// <summary>
        /// Full path
        /// </summary>
        [JsonIgnore]
        public string Path => m_Path ?? string.Empty;

        [JsonProperty("path")]
        internal string PathForJson
        {
            get
            {
                if (string.IsNullOrEmpty(m_Path))
                    return null;
                return m_Path.Replace(EditorApplication.applicationContentsPath,
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

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="path">File path</param>
        public Location(string path)
        {
            m_Path = path;
        }

        /// <summary>
        /// Constructor with line number
        /// </summary>
        /// <param name="path">File path</param>
        /// <param name="line">Line number</param>
        [JsonConstructor]
        public Location(string path, int line)
        {
            m_Path = path;
            m_Line = line;
        }

        /// <summary>
        /// Checks whether the location is valid
        /// </summary>
        /// <returns>True if the location is valid</returns>
        public bool IsValid()
        {
            return !string.IsNullOrEmpty(m_Path);
        }

        string GetFormattedPath(string path)
        {
            if (path.EndsWith(".cs"))
                return $"{path}:{m_Line}";
            return path;
        }
    }
}
