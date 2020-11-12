using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using UnityEditor;
using UnityEngine;

namespace Unity.ProjectAuditor.Editor
{
    /// <summary>
    /// ProjectReport contains a list of all issues found by ProjectAuditor
    /// </summary>
    [Serializable]
    public class ProjectReport
    {
        [SerializeField] List<ProjectIssue> m_Issues = new List<ProjectIssue>();
        static Mutex s_Mutex = new Mutex();

        public int NumTotalIssues
        {
            get { return m_Issues.Count; }
        }

        /// <summary>
        /// Get total number of issues for a specific IssueCategory
        /// </summary>
        /// <param name="category"> Desired IssueCategory</param>
        /// <returns> Number of project issues</returns>
        public int GetNumIssues(IssueCategory category)
        {
            s_Mutex.WaitOne();
            var result = m_Issues.Count(i => i.category == category);
            s_Mutex.ReleaseMutex();
            return result;
        }

        /// <summary>
        /// Get all issues for a specific IssueCategory
        /// </summary>
        /// <param name="category"> Desired IssueCategory</param>
        /// <returns> Array of project issues</returns>
        public ProjectIssue[] GetIssues(IssueCategory category)
        {
            s_Mutex.WaitOne();
            var result = m_Issues.Where(i => i.category == category).ToArray();
            s_Mutex.ReleaseMutex();
            return result;
        }

        internal void AddIssue(ProjectIssue projectIssue)
        {
            s_Mutex.WaitOne();
            m_Issues.Add(projectIssue);
            s_Mutex.ReleaseMutex();
        }

        /// <summary>
        /// Export report to CSV format
        /// </summary>
        public void ExportToCSV(string reportPath, Func<ProjectIssue, bool> match = null)
        {
            var writer = new StreamWriter(reportPath);
            writer.WriteLine(HeaderForCSV());

            for (IssueCategory category = 0; category < IssueCategory.NumCategories; category++)
            {
                var issues = GetIssues(category).Where(i => match == null || match(i));

                foreach (var issue in issues)
                {
                    writer.WriteLine(FormatIssueForCSV(issue));
                }
            }

            writer.Flush();
            writer.Close();

            EditorUtility.RevealInFinder(reportPath);
        }

        internal static string FormatIssueForCSV(ProjectIssue issue)
        {
            if (issue.category == IssueCategory.Code)
                return string.Format("{0},\"{1}\",\"{2}\",{3},{4}:{5}", issue.category, issue.descriptor.description,
                    issue.description,
                    issue.descriptor.area, issue.relativePath, issue.line);
            return string.Format("{0},\"{1}\",\"{2}\",{3},{4}", issue.category, issue.descriptor.description,
                issue.description,
                issue.descriptor.area, issue.relativePath);
        }

        internal static string HeaderForCSV()
        {
            return "Category,Issue,Description,Area,Path";
        }
    }
}
