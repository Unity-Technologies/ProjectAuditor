using System.Collections.Generic;
using System.IO;

namespace Unity.ProjectAuditor.Editor
{
    public class ProjectReport
    {
        private Dictionary<IssueCategory, List<ProjectIssue>> m_IssueDict = new Dictionary<IssueCategory, List<ProjectIssue>>();

        public List<ProjectIssue> GetIssues(IssueCategory category)
        {
            return m_IssueDict[category];  
        }

        public void AddIssue(ProjectIssue projectIssue, IssueCategory category)
        {
            if (!m_IssueDict.ContainsKey(category))
                m_IssueDict.Add(category, new List<ProjectIssue>());
            
            m_IssueDict[category].Add(projectIssue);
        }
        
        public void WriteToFile()
        {
            foreach (var issues in m_IssueDict)
            {
                var json = JsonHelper.ToJson<ProjectIssue>(issues.Value.ToArray(), true);
                File.WriteAllText("Report_" + issues.Key.ToString() + ".json", json);
            }
        }
    }
}