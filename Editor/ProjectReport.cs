using System.Collections.Generic;
using System.IO;

namespace Unity.ProjectAuditor.Editor
{
    public class ProjectReport
    {
        private List<ProjectIssue> m_ApiCallsIssues = new List<ProjectIssue>();
        private List<ProjectIssue> m_ProjectSettingsIssues = new List<ProjectIssue>();

        public List<ProjectIssue> GetIssues(IssueCategory category)
        {
            return category == IssueCategory.ApiCalls
                ? m_ApiCallsIssues
                : m_ProjectSettingsIssues;
        }

        public void AddIssue(ProjectIssue projectIssue, IssueCategory category)
        {
            var issues = category == IssueCategory.ApiCalls
                ? m_ApiCallsIssues
                : m_ProjectSettingsIssues;
            
            issues.Add(projectIssue);
        }
        
        public void WriteToFile()
        {
            
            var json = JsonHelper.ToJson<ProjectIssue>(m_ApiCallsIssues.ToArray(), true);
            File.WriteAllText("Report_ApiCalls.json", json);
            json = JsonHelper.ToJson<ProjectIssue>(m_ProjectSettingsIssues.ToArray(), true);
            File.WriteAllText("Report_ProjectSettings.json", json);
        }
    }
}