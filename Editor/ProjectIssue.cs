using System;
using UnityEngine;

namespace Unity.ProjectAuditor.Editor
{
    enum Area{
        CPU,
        GPU,
        Memory,
        BuildSize,
        LoadTimes
    }

    public enum IssueCategory
    {
        ApiCalls,
        ProjectSettings,
    }
    
    [Serializable]
    public class ProjectIssue
    {
        public ProblemDescriptor descriptor;
        public string description;
        public string callingMethod;
        public IssueCategory category;
        public string url;
        public int line;
        public int column;
        public Rule.Action action;

        public string location
        {
            get
            {                
                return string.IsNullOrEmpty(url) ? String.Empty : string.Format("{0}({1},{2})", url, line, column);
            }
        }

        public string relativePath
        {
            get
            {
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
    }
}