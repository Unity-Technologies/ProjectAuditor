using System;
using UnityEngine;

namespace Unity.ProjectAuditor.Editor.Utils
{
    [Serializable]
    public class Location
    {
        [SerializeField] int m_Line;
        [SerializeField] string m_Path; // path relative to the project folder

        public string Extension
        {
            get
            {
                return System.IO.Path.GetExtension(m_Path) ?? string.Empty;
            }
        }

        public string Filename
        {
            get { return string.IsNullOrEmpty(m_Path) ? string.Empty : System.IO.Path.GetFileName(m_Path); }
        }

        public string FormattedFilename
        {
            get { return GetFormattedPath(Filename); }
        }

        public string FormattedPath
        {
            get { return GetFormattedPath(Path); }
        }

        public int Line
        {
            get { return m_Line; }
        }

        public string Path
        {
            get
            {
                return m_Path ?? string.Empty;
            }
        }

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
                return string.Format("{0}:{1}", path, m_Line);
            return path;
        }
    }
}
