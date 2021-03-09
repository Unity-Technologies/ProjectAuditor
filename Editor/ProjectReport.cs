using System;
using System.Collections.Generic;
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
        [SerializeField] DateTime m_DateTime;
        [SerializeField] string m_HostName;
        [SerializeField] string m_HostPlatform;
        [SerializeField] string m_PackageVersion;
        [SerializeField] BuildTarget m_Platform;
        [SerializeField] string m_UnityVersion;

        // report data
        [SerializeField] List<ProjectIssue> m_Issues = new List<ProjectIssue>();

        static Mutex s_Mutex = new Mutex();

        public int NumTotalIssues
        {
            get { return m_Issues.Count; }
        }

        public ProjectReport()
        {
        }

        public ProjectReport(BuildTarget platform)
        {
            m_DateTime = DateTime.Now;
            m_HostName = SystemInfo.deviceName;
            m_HostPlatform = SystemInfo.operatingSystem;

            var packageInfo = UnityEditor.PackageManager.PackageInfo.FindForAssetPath("Packages/com.unity.project-auditor/Editor/Unity.ProjectAuditor.Editor.asmdef");
            m_PackageVersion = packageInfo.version;
            m_Platform = platform;
            m_UnityVersion = Application.unityVersion;
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

        internal void ExportToCSV(string path, IssueLayout layout, Func<ProjectIssue, bool> match = null)
        {
            using (var exporter = new Exporter(path, layout))
            {
                exporter.WriteHeader();
                foreach (var issue in m_Issues.Where(i => i.category == layout.category))
                    exporter.WriteIssue(issue);
            }
        }
    }
}
