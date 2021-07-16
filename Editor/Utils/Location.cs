using System;
using UnityEngine;

namespace Unity.ProjectAuditor.Editor.Utils
{
    [Serializable]
    public class Location
    {
        [SerializeField] int m_Line;
        [SerializeField] string m_Path; // path relative to the project folder

        public string Filename
        {
            get { return string.IsNullOrEmpty(m_Path) ? string.Empty : System.IO.Path.GetFileName(m_Path); }
        }

        public int Line
        {
            get { return m_Line; }
        }

        public string Extension
        {
            get
            {
                if (string.IsNullOrEmpty(m_Path))
                    return string.Empty;
                return System.IO.Path.GetExtension(m_Path);
            }
        }

        public string Path
        {
            get
            {
                if (string.IsNullOrEmpty(m_Path))
                    return string.Empty;
                return m_Path;
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
    }
}
