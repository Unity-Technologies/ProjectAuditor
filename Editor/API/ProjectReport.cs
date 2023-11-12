using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Unity.ProjectAuditor.Editor.Core;
using Unity.ProjectAuditor.Editor.Diagnostic;
using UnityEngine;
using Newtonsoft.Json;
using UnityEngine.Serialization;

namespace Unity.ProjectAuditor.Editor
{
    // stephenm TODO: This whole class needs proper documentation comments
    [Serializable]
    public class SessionInfo : AnalysisParams
    {
        // for serialization purposes only
        public SessionInfo() : base(false) {}

        public SessionInfo(AnalysisParams serializedParams)
            : base(serializedParams)
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
    public sealed class ProjectReport
    {
        const string k_CurrentVersion = "0.2";

        [JsonProperty("version")]
        [SerializeField]
        public string Version = k_CurrentVersion;

        // stephenm TODO: ModuleInfo serializes to JSON but isn't accessible in any meaningful way if a script just has a ProjectReport object it wants to query. Figure out some API for this?
        // stephenm TODO: Keeping this internal for now. Exposing this means exposing IssueLayout, which means exposing PropertyDefinition, which to be useful means exposing every enum that can
        // be passed to PropertyTypeUtil.FromCustom() (basically one per view). I'd love to find a more elegant way to do this.
        [Serializable]
        class ModuleInfo
        {
            // stephenm TODO: Comment (for all these fields... Assuming we do what the above comment says and expose this via an API of some sort)
            public string name;

            // this is used by HasCategory
            public IssueCategory[] categories;
            public IReadOnlyCollection<IssueLayout> layouts;

            public string startTime;
            public string endTime;
        }

        // stephenm TODO: Check the serialization here. Changed this from private SessionInfo m_SessionInfo with a property but I don't see why this shouldn't work?
        [JsonProperty("sessionInfo")]
        [SerializeField]
        public SessionInfo SessionInfo;

        [JsonProperty("moduleMetadata")]
        [SerializeField]
        List<ModuleInfo> m_ModuleInfos = new List<ModuleInfo>();

        // stephenm TODO: Should we be able to access the DescriptorLibrary here? It serialises to JSON, but I'm leaning towards "no".
        [SerializeField]
        DescriptorLibrary m_DescriptorLibrary = new DescriptorLibrary();

        [JsonIgnore]
        [SerializeField]
        List<ProjectIssue> m_Issues = new List<ProjectIssue>();

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

        // stephenm TODO: comment
        [JsonIgnore]
        public int NumTotalIssues => m_Issues.Count;

        // stephenm TODO: comment
        // for serialization purposes only
        internal ProjectReport()
        {}

        // for internal use only
        internal ProjectReport(AnalysisParams analysisParams)
        {
            SessionInfo = new SessionInfo(analysisParams)
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

        // stephenm TODO: comment
        public bool HasCategory(IssueCategory category)
        {
            return category == IssueCategory.Metadata || m_ModuleInfos.Any(m => m.categories.Contains(category));
        }

        // stephenm TODO: comment
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

        // stephenm TODO: comment
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

        // stephenm TODO: comment
        public bool IsValid()
        {
            return m_Issues.All(i => i.IsValid());
        }

        // stephenm TODO: comment
        public void Save(string path)
        {
            File.WriteAllText(path,
                JsonConvert.SerializeObject(this, UserPreferences.PrettifyJsonOutput ? Formatting.Indented : Formatting.None,
                    new JsonSerializerSettings
                    {
                        NullValueHandling = NullValueHandling.Ignore
                    }));
        }

        // stephenm TODO: comment
        public static ProjectReport Load(string path)
        {
            return JsonConvert.DeserializeObject<ProjectReport>(File.ReadAllText(path), new JsonSerializerSettings
            {
                ObjectCreationHandling = ObjectCreationHandling.Replace
            });
        }

        // stephenm TODO: I'm keeping these specialist export methods internal for now. I don't know if they should be accessible/included in PA 1.0,
        // and if they should, it means also exposing IssueLayout and the data types it uses, which opens a whole can of worms.
        internal void ExportToCsv(string path, IssueLayout layout, Func<ProjectIssue, bool> predicate = null)
        {
            var issues = m_Issues.Where(i => i.category == layout.category && (predicate == null || predicate(i))).ToArray();
            using (var exporter = new CsvExporter(path, layout))
            {
                exporter.WriteHeader();
                exporter.WriteIssues(issues);
            }
        }

        // stephenm TODO: I'm keeping these specialist export methods internal for now. I don't know if they should be accessible/included in PA 1.0,
        // and if they should, it means also exposing IssueLayout and the data types it uses, which opens a whole can of worms.
        internal void ExportToHtml(string path, IssueLayout layout, Func<ProjectIssue, bool> predicate = null)
        {
            var issues = m_Issues.Where(i => i.category == layout.category && (predicate == null || predicate(i))).ToArray();
            using (var exporter = new HtmlExporter(path, layout))
            {
                exporter.WriteHeader();
                exporter.WriteIssues(issues);
                exporter.WriteFooter();
            }
        }

        // Internal only: Data written by ProjectAuditor during analysis
        internal void RecordModuleInfo(Module module, DateTime startTime, DateTime endTime)
        {
            var name = module.Name;
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
                    name = module.Name,
                    categories = module.Categories,
                    layouts = module.SupportedLayouts,
                    startTime = Utils.Json.SerializeDateTime(startTime),
                    endTime = Utils.Json.SerializeDateTime(endTime)
                });
            }
        }

        // Internal only: Data written by ProjectAuditor during analysis
        internal void AddIssues(IEnumerable<ProjectIssue> issues)
        {
            s_Mutex.WaitOne();
            m_Issues.AddRange(issues);
            s_Mutex.ReleaseMutex();
        }
    }
}
