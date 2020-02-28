using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Unity.ProjectAuditor.Editor
{
    [Serializable]
    public class ProjectReport
    {
        [SerializeField] private List<ProjectIssue> m_Issues = new List<ProjectIssue>();

        public int NumTotalIssues
        {
            get { return m_Issues.Count; }
        }

        public int GetNumIssues(IssueCategory category)
        {
            return m_Issues.Count(i => i.category == category);
        }

        public IEnumerable<ProjectIssue> GetIssues(IssueCategory category)
        {
            return m_Issues.Where(i => i.category == category);
        }

        public void AddIssue(ProjectIssue projectIssue)
        {
            m_Issues.Add(projectIssue);
        }

        public void Export(string reportPath)
        {
            var writer = new StreamWriter(reportPath);
            writer.WriteLine("Issue,Message,Area,Path");

            for (IssueCategory category = 0; category < IssueCategory.NumCategories; category++)
            {
                var issues = GetIssues(category);

                foreach (var issue in issues)
                {
                    var path = issue.relativePath;
                    if (category != IssueCategory.ProjectSettings)
                        path += ":" + issue.line;
                    writer.WriteLine(
                        issue.descriptor.description + "," +
                        issue.description + "," +
                        issue.descriptor.area + "," +
                        path
                    );
                }
            }

            writer.Flush();
            writer.Close();
        }
    }
}