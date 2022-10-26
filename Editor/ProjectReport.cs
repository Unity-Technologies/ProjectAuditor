using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Unity.ProjectAuditor.Editor.Core;
using Unity.ProjectAuditor.Editor.Utils;
using UnityEngine;

namespace Unity.ProjectAuditor.Editor
{
    /// <summary>
    /// ProjectReport contains a list of all issues found by ProjectAuditor
    /// </summary>
    [Serializable]
    public sealed class ProjectReport
    {
        [Serializable]
        class ModuleInfo
        {
            public string name;
            public DateTime startTime;
            public DateTime endTime;
        }

        [SerializeField] List<ModuleInfo> m_ModuleInfos = new List<ModuleInfo>();

        [SerializeField] List<ProjectIssue> m_Issues = new List<ProjectIssue>();

        static Mutex s_Mutex = new Mutex();

        public int NumTotalIssues => m_Issues.Count;

        // for internal use only
        internal ProjectReport()
        {}

        public void RecordModuleInfo(string name, DateTime startTime, DateTime endTime)
        {
            var info = m_ModuleInfos.FirstOrDefault(m => m.name.Equals(name));
            if (info != null)
            {
                info.startTime = startTime;
                info.endTime = endTime;
            }
            else
            {
                m_ModuleInfos.Add(new ModuleInfo
                {
                    name = name,
                    startTime = startTime,
                    endTime = endTime
                });
            }
        }

        public IReadOnlyCollection<ProjectIssue> GetAllIssues()
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
        public IReadOnlyCollection<ProjectIssue> GetIssues(IssueCategory category)
        {
            s_Mutex.WaitOne();
            var result = m_Issues.Where(i => i.category == category).ToArray();
            s_Mutex.ReleaseMutex();
            return result;
        }

        internal void AddIssues(IEnumerable<ProjectIssue> issues)
        {
            s_Mutex.WaitOne();
            m_Issues.AddRange(issues);
            s_Mutex.ReleaseMutex();
        }

        internal void ClearIssues(IssueCategory category)
        {
            s_Mutex.WaitOne();
            m_Issues.RemoveAll(issue => issue.category == category);
            s_Mutex.ReleaseMutex();
        }

        public void ExportToCSV(string path, IssueLayout layout, Func<ProjectIssue, bool> predicate = null)
        {
            var issues = m_Issues.Where(i => i.category == layout.category && (predicate == null || predicate(i))).ToArray();
            using (var exporter = new CSVExporter(path, layout))
            {
                exporter.WriteHeader();
                exporter.WriteIssues(issues);
            }
        }

        public void ExportToHTML(string path, IssueLayout layout, Func<ProjectIssue, bool> predicate = null)
        {
            var issues = m_Issues.Where(i => i.category == layout.category && (predicate == null || predicate(i))).ToArray();
            using (var exporter = new HTMLExporter(path, layout))
            {
                exporter.WriteHeader();
                exporter.WriteIssues(issues);
                exporter.WriteFooter();
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
