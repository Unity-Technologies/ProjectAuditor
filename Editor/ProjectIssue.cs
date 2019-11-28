using System;
using System.IO;
using UnityEngine;

namespace Unity.ProjectAuditor.Editor
{
    public enum Area{
        CPU,
        GPU,
        Memory,
        BuildSize,
        LoadTimes,
        
        All,        
    }

    public enum IssueCategory
    {
        ApiCalls,
        ProjectSettings,
        NumCategories
    }

    [Serializable]
    public class ProjectIssue
    {
        public ProblemDescriptor descriptor;
		public CallTreeNode callTree;
        public IssueCategory category;
        public string url;
        public int line;
        public int column;
        public string assembly;

        public string description
        {
            get { return descriptor.description; }
        }
        
        public string filename
        {
            get
            {
                if (string.IsNullOrEmpty(url))
                    return String.Empty;
                return Path.GetFileName(url);
            }
        }

        public string relativePath
        {
            get
            {
                if (string.IsNullOrEmpty(url))
                    return String.Empty;

                string path = url;
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

        public string callingMethod
        {
            get
            {
                if (callTree == null)
                    return string.Empty;
                if (!callTree.HasChildren())
                    return string.Empty;

                return callTree.GetChild().name;
            }
        }
        
        public string name
        {
            get
            {
                if (callTree == null)
                    return string.Empty;
                if (callTree.prettyName.Equals(descriptor.description))
                {
                    // if name matches the descriptor's name, use caller's name instead
                    return string.IsNullOrEmpty(callingMethod) ? string.Empty : callTree.GetChild().prettyName;
                }
                else
                {
                    return callTree.prettyName;
                }
            }
        }
    }
}
