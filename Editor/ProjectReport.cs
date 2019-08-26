using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Unity.ProjectAuditor.Editor
{
    public class ProjectReport
    {
        public List<ProjectIssue> m_ApiCallsIssues = new List<ProjectIssue>();
        public List<ProjectIssue> m_ProjectSettingsIssues = new List<ProjectIssue>();

        public void WriteToFile()
        {
            
            var json = JsonHelper.ToJson<ProjectIssue>(m_ApiCallsIssues.ToArray(), true);
            File.WriteAllText("Report_ApiCalls.json", json);
            json = JsonHelper.ToJson<ProjectIssue>(m_ProjectSettingsIssues.ToArray(), true);
            File.WriteAllText("Report_ProjectSettings.json", json);
        }
    }
}