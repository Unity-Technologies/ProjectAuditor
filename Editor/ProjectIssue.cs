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
        public string description;
		public CallTreeNode callTree;
        public IssueCategory category;
        public string url;
        public int line;
        public int column;
        public string assembly;

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

                return callTree.GetChild().name;
            }
        }
        
        public string callingMethodName
        {
            get
            {
                if (string.IsNullOrEmpty(callingMethod))
                    return string.Empty;
                return callTree.GetChild().prettyName;
            }
        }
    }
}
