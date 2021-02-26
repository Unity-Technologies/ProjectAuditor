using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Editor.Utils;
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

        internal void ClearIssues(IssueCategory category)
        {
            s_Mutex.WaitOne();
            m_Issues.RemoveAll(issue => issue.category == category);
            s_Mutex.ReleaseMutex();
        }

        internal void ExportToCSV(string reportPath, IssueLayout layout, Func<ProjectIssue, bool> match = null)
        {
            var path = string.Format("{0}-{1}.csv", reportPath, layout.category.ToString()).ToLower();
            using (var exporter = new Exporter(path, layout))
            {
                exporter.WriteHeader();
                foreach (var issue in m_Issues.Where(i => i.category == layout.category))
                    exporter.WriteIssue(issue);
            }
        }
    }
}
