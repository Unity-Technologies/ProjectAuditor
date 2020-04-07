using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using UnityEngine;

namespace Unity.ProjectAuditor.Editor
{
    [Serializable]
    public class ProjectReport
    {
        [SerializeField] private List<ProjectIssue> m_Issues = new List<ProjectIssue>();
        private static Mutex mutex = new Mutex();

        public int NumTotalIssues
        {
            get { return m_Issues.Count; }
        }

        public int GetNumIssues(IssueCategory category)
        {
            mutex.WaitOne();
            var result = m_Issues.Count(i => i.category == category);
            mutex.ReleaseMutex();
            return result;
        }

        public ProjectIssue[] GetIssues(IssueCategory category)
        {
            mutex.WaitOne();
            var result = m_Issues.Where(i => i.category == category).ToArray();
            mutex.ReleaseMutex();
            return result;
        }

        public void AddIssue(ProjectIssue projectIssue)
        {
            mutex.WaitOne();
            m_Issues.Add(projectIssue);
            mutex.ReleaseMutex();
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
