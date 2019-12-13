using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Unity.ProjectAuditor.Editor.Utils
{
    [Serializable]
    public class Location
    {
        public string path;
        public int line;
        
        public string filename
        {
            get
            {
                return string.IsNullOrEmpty(path) ? string.Empty : Path.GetFileName(path);
            }
        }

        public string relativePath
        {
            get
            {
                if (string.IsNullOrEmpty(this.path))
                    return string.Empty;

                string path = this.path;
                if (path.Contains("BuiltInPackages"))
                {
                    path = path.Remove(0, path.IndexOf("BuiltInPackages") + "BuiltInPackages/".Length);                        
                }
                else
                {
                    var projectPathLength = Application.dataPath.Length - "Assets".Length;
                    if (path.Length > projectPathLength)
                        path = path.Remove(0, projectPathLength);                         
                }

                return path;
            }
        }

        public bool IsValid()
        {
            return !string.IsNullOrEmpty(path);
        }
        
        public void Open()
        {
            var path = relativePath;
            if (!string.IsNullOrEmpty(path))
            {
                if ((path.StartsWith("Library/PackageCache") || path.StartsWith("Packages/") && path.Contains("@")))
                {
                    // strip version from package path
                    var version = path.Substring(path.IndexOf("@"));
                    version = version.Substring(0, version.IndexOf("/"));
                    path = path.Replace(version, "").Replace("Library/PackageCache", "Packages");
                }

                var obj = AssetDatabase.LoadAssetAtPath<TextAsset>(path);
                AssetDatabase.OpenAsset(obj, line);
            }           
        }
    }
}