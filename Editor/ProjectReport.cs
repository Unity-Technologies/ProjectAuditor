using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Unity.ProjectAuditor.Editor.Utils;
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

        public ProjectIssue[] GetAllIssues()
        {
            s_Mutex.WaitOne();
            var result = m_Issues.ToArray();
            s_Mutex.ReleaseMutex();
            return result;
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

        public void ExportToCSV(string path, IssueLayout layout, Func<ProjectIssue, bool> match = null)
        {
            using (var exporter = new Exporter(path, layout))
            {
                exporter.WriteHeader();
                exporter.WriteIssues(m_Issues.Where(i => i.category == layout.category).ToArray());
            }
        }

        public void Save(string path)
        {
            File.WriteAllText(path, JsonUtility.ToJson(this));
        }

        public static ProjectReport Load(string path)
        {
            return JsonUtility.FromJson<ProjectReport>(File.ReadAllText(path));
        }
    }
}
