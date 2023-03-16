using System;
using UnityEngine;

namespace Unity.ProjectAuditor.Editor
{
    [Serializable]
    public class Location
    {
        [SerializeField] int m_Line;
        [SerializeField] string m_Path; // path relative to the project folder

        public string Extension => System.IO.Path.GetExtension(m_Path) ?? string.Empty;

        public string Filename => string.IsNullOrEmpty(m_Path) ? string.Empty : System.IO.Path.GetFileName(m_Path);

        public string FormattedFilename => GetFormattedPath(Filename);

        public string FormattedPath => GetFormattedPath(Path);

        public int Line => m_Line;

        public string Path => m_Path ?? string.Empty;

        public Location(string path)
        {
            m_Path = path;
        }

        public Location(string path, int line)
        {
            m_Path = path;
            m_Line = line;
        }

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
