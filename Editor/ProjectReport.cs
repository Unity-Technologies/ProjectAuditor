using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Unity.ProjectAuditor.Editor.Core;
using Unity.ProjectAuditor.Editor.Diagnostic;
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
            public IssueCategory[] categories;
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

        internal void RecordModuleInfo(ProjectAuditorModule module, DateTime startTime, DateTime endTime)
        {
            var name = module.name;
            var info = m_ModuleInfos.FirstOrDefault(m => m.name.Equals(name));
            if (info != null)
            {
                info.name = module.name;
                info.categories = module.categories;
                info.startTime = startTime;
                info.endTime = endTime;
            }
            else
            {
                m_ModuleInfos.Add(new ModuleInfo
                {
                    name = module.name,
                    categories = module.categories,
                    startTime = startTime,
                    endTime = endTime
                });
            }
        }

        public bool HasCategory(IssueCategory category)
        {
            return m_ModuleInfos.Any(m => m.categories.Contains(category));
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
        /// find all issues for a specific IssueCategory
        /// </summary>
        /// <param name="category"> Desired IssueCategory</param>
        /// <returns> Array of project issues</returns>
        public IReadOnlyCollection<ProjectIssue> FindByCategory(IssueCategory category)
        {
            s_Mutex.WaitOne();
            var result = m_Issues.Where(i => i.category == category).ToArray();
            s_Mutex.ReleaseMutex();
            return result;
        }

        /// <summary>
        /// find all diagnostics of a specific descriptor
        /// </summary>
        /// <param name="descriptor"> Desired Descriptor</param>
        /// <returns> Array of project issues</returns>
        public IReadOnlyCollection<ProjectIssue> FindByDescriptor(Descriptor descriptor)
        {
            s_Mutex.WaitOne();
            var result = m_Issues.Where(i => i.descriptor != null && i.descriptor.Equals(descriptor)).ToArray();
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
            foreach (var info in m_ModuleInfos)
            {
                var categories = info.categories.ToList();
                categories.RemoveAll(c => c == category);
                info.categories = categories.ToArray();
            }
            m_ModuleInfos.RemoveAll(info => info.categories.Length == 0);
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
