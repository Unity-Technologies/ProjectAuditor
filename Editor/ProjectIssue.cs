using System;

namespace Unity.ProjectAuditor.Editor
{
    enum IssueCategory
    {
        ApiCalls,
        ProjectSettings
    }
    
    [Serializable]
    public class ProjectIssue
    {
        public ProblemDefinition def;
        public string category;
        public string url;
        public int line;
        public int column;
        public bool resolved;

        public string location
        {
            get
            {                
                return string.IsNullOrEmpty(url) ? String.Empty : $"{url}({line},{column})";
            }
        }
    }
}