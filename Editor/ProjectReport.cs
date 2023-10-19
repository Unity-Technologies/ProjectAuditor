using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Unity.ProjectAuditor.Editor.Core;
using Unity.ProjectAuditor.Editor.Diagnostic;
using UnityEngine;
using Newtonsoft.Json;
using UnityEditor;

namespace Unity.ProjectAuditor.Editor
{
    [Serializable]
    internal class SessionInfo : ProjectAuditorParams
    {
        // for serialization purposes only
        public SessionInfo()
            : base(new ProjectAuditorParams(BuildTarget.NoTarget, UserPreferences.RulesAssetPath))
        {}

        public SessionInfo(ProjectAuditorParams projectAuditorParams)
            : base(projectAuditorParams)
        {}

        public string ProjectAuditorVersion;
        public string UnityVersion;

        public string CompanyName;
        public string ProjectId;
        public string ProjectName;
        public string ProjectRevision;

        public string DateTime;
        public string HostName;
        public string HostPlatform;

        public bool UseRoslynAnalyzers;
    }

    /// <summary>
    /// ProjectReport contains a list of all issues found by ProjectAuditor
    /// </summary>
    [Serializable]
    internal sealed class ProjectReport
    {
        internal const string k_CurrentVersion = "0.2";

        [JsonProperty("version")]
        [SerializeField]
        string m_Version = k_CurrentVersion;

        [Serializable]
        class ModuleInfo
        {
            public string name;

            // this is used by HasCategory
            public IssueCategory[] categories;
            public IReadOnlyCollection<IssueLayout> layouts;

            public string startTime;
            public string endTime;
        }

        [JsonProperty("sessionInfo")]
        SessionInfo m_SessionInfo;

        [SerializeField]
        [JsonProperty("moduleMetadata")]
        List<ModuleInfo> m_ModuleInfos = new List<ModuleInfo>();

        [SerializeField]
        DescriptorLibrary m_DescriptorLibrary = new DescriptorLibrary();

        [JsonIgnore]
        [SerializeField]
        List<ProjectIssue> m_Issues = new List<ProjectIssue>();

        [JsonIgnore]
        public SessionInfo SessionInfo => m_SessionInfo;

        [JsonProperty("issues")]
        internal ProjectIssue[] UnfixedIssues
        {
            get
            {
                return m_Issues.Where(i => !i.wasFixed).ToArray();
            }
            set => m_Issues = value.ToList();
        }

        static Mutex s_Mutex = new Mutex();

        [JsonProperty("descriptors")]
        internal List<Descriptor> Descriptors
        {
            get
            {
                m_DescriptorLibrary.OnBeforeSerialize();
                return m_DescriptorLibrary.m_SerializedDescriptors;
            }
            set
            {
                m_DescriptorLibrary.m_SerializedDescriptors = value;
                m_DescriptorLibrary.OnAfterDeserialize();
            }
        }

        [JsonIgnore]
        public int NumTotalIssues => m_Issues.Count;

        [JsonIgnore]
        public string Version => m_Version;

        // for serialization purposes only
        internal ProjectReport()
        {}

        // for internal use only
        internal ProjectReport(ProjectAuditorParams projectAuditorParams)
        {
            m_SessionInfo = new SessionInfo(projectAuditorParams)
            {
                ProjectAuditorVersion = ProjectAuditor.PackageVersion,

                ProjectId = Application.cloudProjectId,
                ProjectName = Application.productName,
                ProjectRevision = "Unknown",
                CompanyName = Application.companyName,
                UnityVersion = Application.unityVersion,

                DateTime = Utils.Json.SerializeDateTime(DateTime.Now),
                HostName = SystemInfo.deviceName,
                HostPlatform = SystemInfo.operatingSystem,

                UseRoslynAnalyzers = UserPreferences.UseRoslynAnalyzers
            };
        }

        public void RecordModuleInfo(ProjectAuditorModule module, DateTime startTime, DateTime endTime)
        {
            var name = module.name;
            var info = m_ModuleInfos.FirstOrDefault(m => m.name.Equals(name));
            if (info != null)
            {
                info.startTime = Utils.Json.SerializeDateTime(startTime);
                info.endTime = Utils.Json.SerializeDateTime(endTime);
            }
            else
            {
                m_ModuleInfos.Add(new ModuleInfo
                {
                    name = module.name,
                    categories = module.categories,
                    layouts = module.supportedLayouts,
                    startTime = Utils.Json.SerializeDateTime(startTime),
                    endTime = Utils.Json.SerializeDateTime(endTime)
                });
            }
        }

        public bool HasCategory(IssueCategory category)
        {
            return category == IssueCategory.Metadata || m_ModuleInfos.Any(m => m.categories.Contains(category));
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
        /// Find all diagnostics that match a specific ID
        /// </summary>
        /// <param name="id"> Desired diagnostic ID</param>
        /// <returns> Array of project issues</returns>
        public IReadOnlyCollection<ProjectIssue> FindByDiagnosticID(string id)
        {
            s_Mutex.WaitOne();
            var result = m_Issues.Where(i => i.id.IsValid() && i.id.Equals(id)).ToArray();
            s_Mutex.ReleaseMutex();
            return result;
        }

        public void AddIssues(IEnumerable<ProjectIssue> issues)
        {
            s_Mutex.WaitOne();
            m_Issues.AddRange(issues);
            s_Mutex.ReleaseMutex();
        }

        public void ClearIssues(IssueCategory category)
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
            using (var exporter = new CsvExporter(path, layout))
            {
                exporter.WriteHeader();
                exporter.WriteIssues(issues);
            }
        }

        public bool IsValid()
        {
            return m_Issues.All(i => i.IsValid());
        }

        public void ExportToHTML(string path, IssueLayout layout, Func<ProjectIssue, bool> predicate = null)
        {
            var issues = m_Issues.Where(i => i.category == layout.category && (predicate == null || predicate(i))).ToArray();
            using (var exporter = new HtmlExporter(path, layout))
            {
                exporter.WriteHeader();
                exporter.WriteIssues(issues);
                exporter.WriteFooter();
            }
        }

        public void Save(string path)
        {
            File.WriteAllText(path,
                JsonConvert.SerializeObject(this, UserPreferences.PrettifyJsonOutput ? Formatting.Indented : Formatting.None,
                    new JsonSerializerSettings
                    {
                        NullValueHandling = NullValueHandling.Ignore
                    }));
        }

        public static ProjectReport Load(string path)
        {
            return JsonConvert.DeserializeObject<ProjectReport>(File.ReadAllText(path));
        }
    }
}
