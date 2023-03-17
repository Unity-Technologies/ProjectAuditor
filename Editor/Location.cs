using System;
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
        public string Extension => System.IO.Path.GetExtension(m_Path) ?? string.Empty;

        /// <summary>
        /// Filename
        /// </summary>
        public string Filename => string.IsNullOrEmpty(m_Path) ? string.Empty : System.IO.Path.GetFileName(m_Path);

        /// <summary>
        /// Formatted filename with line number
        /// </summary>
        public string FormattedFilename => GetFormattedPath(Filename);

        /// <summary>
        /// Formatted path with line number
        /// </summary>
        public string FormattedPath => GetFormattedPath(Path);

        /// <summary>
        /// Line number
        /// </summary>
        public int Line => m_Line;

        /// <summary>
        /// Full path
        /// </summary>
        public string Path => m_Path ?? string.Empty;

        /// <summary>
        /// Constructor
        /// </summary>
        /// /// <param name="path">File path</param>
        public Location(string path)
        {
            m_Path = path;
        }

        /// <summary>
        /// Constructor with line number
        /// </summary>
        /// <param name="path">File path</param>
        /// <param name="line">Line number</param>
        public Location(string path, int line)
        {
            m_Path = path;
            m_Line = line;
        }

        /// <summary>
        /// Checks whether the location is valid
        /// </summary>
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
