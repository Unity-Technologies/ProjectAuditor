using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Unity.ProjectAuditor.Editor.Core;
using Unity.ProjectAuditor.Editor.Diagnostic;
using UnityEngine;
using Newtonsoft.Json;

namespace Unity.ProjectAuditor.Editor
{
    /// <summary>
    /// Contains information about the session in which a <seealso cref="ProjectReport"/> was created.
    /// </summary>
    [Serializable]
    public class SessionInfo : AnalysisParams
    {
        /// <summary>
        /// Default Constructor. For serialization purposes only.
        /// </summary>
        public SessionInfo() : base(false) {}

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="serializedParams">AnalysisParams object which was passed to ProjectAuditor to create the ProjectReport</param>
        public SessionInfo(AnalysisParams serializedParams)
            : base(serializedParams)
        {}

        /// <summary>
        /// The version number of the Project Auditor package which was used.
        /// </summary>
        public string ProjectAuditorVersion;

        /// <summary>
        /// The version of Unity which was used.
        /// </summary>
        public string UnityVersion;

        /// <summary>
        /// The Company Name string in the project's Project Settings.
        /// </summary>
        public string CompanyName;

        /// <summary>
        /// The `Application.cloudProjectId` identifier for the project.
        /// </summary>
        public string ProjectId;

        /// <summary>
        /// The Product Name string in the project's Project Settings.
        /// </summary>
        public string ProjectName;

        /// <summary>
        /// The Product Version string in the project's Project Settings.
        /// </summary>
        public string ProjectRevision;

        /// <summary>
        /// The date and time at which the ProjectReport was created.
        /// </summary>
        public string DateTime;

        /// <summary>
        /// The `SystemInfo.deviceName` identifier for the device on which the Unity Editor was running.
        /// </summary>
        public string HostName;

        /// <summary>
        /// The `SystemInfo.operatingSystem` identifier for the operating system on which the Unity Editor was running.
        /// </summary>
        public string HostPlatform;

        /// <summary>
        /// True if the "Use Roslyn Analyzers" checkbox was ticked in Preferences > Project Auditor.
        /// </summary>
        public bool UseRoslynAnalyzers;
    }

    /// <summary>
    /// ProjectReport contains a list of all issues found by ProjectAuditor.
    /// </summary>
    [Serializable]
    public sealed class ProjectReport
    {
        const string k_CurrentVersion = "0.2";

        [JsonIgnore] [SerializeField]
        string m_Version = k_CurrentVersion;

        // stephenm TODO: ModuleInfo serializes to JSON but isn't accessible in any meaningful way if a script just has a ProjectReport object it wants to query. Figure out some API for this? Phase 2.
        // Keeping this internal for now. Exposing this means exposing IssueLayout, which means exposing PropertyDefinition, which to be useful means exposing every enum that can
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

        /// <summary>
        /// Contains information about the session in which this ProjectReport was created.
        /// </summary>
        [JsonProperty("sessionInfo")] [SerializeField]
        public SessionInfo SessionInfo;

        [JsonProperty("moduleMetadata")] [SerializeField]
        List<ModuleInfo> m_ModuleInfos = new List<ModuleInfo>();

        [SerializeField]
        DescriptorLibrary m_DescriptorLibrary = new DescriptorLibrary();

        [JsonIgnore] [SerializeField]
        List<ProjectIssue> m_Issues = new List<ProjectIssue>();

        [JsonProperty("issues")]
        internal ProjectIssue[] UnfixedIssues
        {
            get
            {
                return m_Issues.Where(i => !i.WasFixed).ToArray();
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

        /// <summary>
        /// The total number of ProjectIssues included in this report.
        /// </summary>
        [JsonIgnore]
        public int NumTotalIssues => m_Issues.Count;

        /// <summary>
        /// File format version of the ProjectReport (read-only).
        /// </summary>
        [JsonProperty("version")]
        public string Version
        {
            get => m_Version;
            internal set => m_Version = value;
        }
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

        /// <summary>
        /// Checks whether the ProjectReport includes analysis for a given IssueCategory.
        /// </summary>
        /// <param name="Category">The IssuesCategory to check</param>
        /// <returns>True if ProjectAuditor ran one or more Modules that reports issues of the specified IssueCategory. Otherwise, returns false.</returns>
        public bool HasCategory(IssueCategory Category)
        {
            return Category == IssueCategory.Metadata || m_ModuleInfos.Any(m => m.categories.Contains(Category));
        }

        /// <summary>
        /// Gets a read-only collection of all of the ProjectIssues included in the report.
        /// </summary>
        /// <returns>All the issues in the report</returns>
        public IReadOnlyCollection<ProjectIssue> GetAllIssues()
        {
            s_Mutex.WaitOne();
            var result = m_Issues.ToArray();
            s_Mutex.ReleaseMutex();
            return result;
        }

        /// <summary>
        /// Get total number of issues for a specific IssueCategory.
        /// </summary>
        /// <param name="Category"> Desired IssueCategory</param>
        /// <returns> Number of project issues</returns>
        public int GetNumIssues(IssueCategory Category)
        {
            s_Mutex.WaitOne();
            var result = m_Issues.Count(i => i.Category == Category);
            s_Mutex.ReleaseMutex();
            return result;
        }

        /// <summary>
        /// find all issues for a specific IssueCategory.
        /// </summary>
        /// <param name="Category"> Desired IssueCategory</param>
        /// <returns> Array of project issues</returns>
        public IReadOnlyCollection<ProjectIssue> FindByCategory(IssueCategory Category)
        {
            s_Mutex.WaitOne();
            var result = m_Issues.Where(i => i.Category == Category).ToArray();
            s_Mutex.ReleaseMutex();
            return result;
        }

        /// <summary>
        /// Find all diagnostics that match a specific ID.
        /// </summary>
        /// <param name="id"> Desired Descriptor ID</param>
        /// <returns> Array of project issues</returns>
        public IReadOnlyCollection<ProjectIssue> FindByDescriptorID(string id)
        {
            s_Mutex.WaitOne();
            var result = m_Issues.Where(i => i.Id.IsValid() && i.Id.Equals(id)).ToArray();
            s_Mutex.ReleaseMutex();
            return result;
        }

        /// <summary>
        /// Clears all issues that match the specified IssueCategory from the report.
        /// </summary>
        /// <param name="Category">The IssueCategory of the issues to remove.</param>
        public void ClearIssues(IssueCategory Category)
        {
            s_Mutex.WaitOne();
            m_Issues.RemoveAll(issue => issue.Category == Category);
            foreach (var info in m_ModuleInfos)
            {
                var categories = info.categories.ToList();
                categories.RemoveAll(c => c == Category);
                info.categories = categories.ToArray();
            }
            m_ModuleInfos.RemoveAll(info => info.categories.Length == 0);
            s_Mutex.ReleaseMutex();
        }

        /// <summary>
        /// Check whether all issues in the report are valid.
        /// </summary>
        /// <returns>True is none of the issues in the report have a null description string. Otherwise returns false.</returns>
        public bool IsValid()
        {
            return m_Issues.All(i => i.IsValid());
        }

        /// <summary>
        /// Save the ProjectReport as a JSON file.
        /// </summary>
        /// <param name="path">The file path at which to save the file</param>
        public void Save(string path)
        {
            File.WriteAllText(path,
                JsonConvert.SerializeObject(this, UserPreferences.PrettifyJsonOutput ? Formatting.Indented : Formatting.None,
                    new JsonSerializerSettings
                    {
                        NullValueHandling = NullValueHandling.Ignore
                    }));
        }

        /// <summary>
        /// Load a ProjectReport from a JSON file at the specified path.
        /// </summary>
        /// <param name="path">File path of the report to load</param>
        /// <returns>A loaded ProjectReport object</returns>
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
            var issues = m_Issues.Where(i => i.Category == layout.category && (predicate == null || predicate(i))).ToArray();
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
            var issues = m_Issues.Where(i => i.Category == layout.category && (predicate == null || predicate(i))).ToArray();
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
