using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Unity.ProjectAuditor.Editor.Utils
{
    public enum LocationType
    {
        Asset,
        Setting
    }

    [Serializable]
    public class Location
    {
        [SerializeField] private int m_Line;
        [SerializeField] private string m_Path; // path relative to the project folder
        [SerializeField] private LocationType m_Type;

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
                if (string.IsNullOrEmpty(this.m_Path))
                    return string.Empty;
                return System.IO.Path.GetExtension(this.m_Path);
            }
        }

        public string Path
        {
            get
            {
                if (string.IsNullOrEmpty(this.m_Path))
                    return string.Empty;
                return m_Path;
            }
        }

        public LocationType Type
        {
            get
            {
                return m_Type;
            }
        }

        internal Location(string path, LocationType type = LocationType.Setting)
        {
            m_Path = path;
            m_Type = type;
        }

        internal Location(string path, int line, LocationType type = LocationType.Asset)
        {
            m_Path = path;
            m_Line = line;
            m_Type = type;
        }

        public bool IsValid()
        {
            return !string.IsNullOrEmpty(m_Path);
        }

        public void Open()
        {
            if (m_Type == LocationType.Setting)
            {
#if UNITY_2018_3_OR_NEWER
                var window = SettingsService.OpenProjectSettings(m_Path);
                window.Repaint();
#endif
            }
            else if (File.Exists(m_Path))
            {
                UnityEngine.Object obj = AssetDatabase.LoadAssetAtPath<TextAsset>(m_Path);
                if (obj == null)
                    obj = AssetDatabase.LoadMainAssetAtPath(m_Path);
                AssetDatabase.OpenAsset(obj, m_Line);
            }
        }
    }
}
