using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Unity.ProjectAuditor.Editor.Utils;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;
#if UNITY_2018_1_OR_NEWER
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

#endif

namespace Unity.ProjectAuditor.Editor
{
    /// <summary>
    /// ProjectAuditor class is responsible for auditing the Unity project
    /// </summary>
    public class ProjectAuditor
#if UNITY_2018_1_OR_NEWER
        : IPreprocessBuildWithReport
#endif
    {
        static readonly Dictionary<string, IssueCategory> s_CustomCategories = new Dictionary<string, IssueCategory>();
        static string s_DataPath;

        internal static string DataPath
        {
            get
            {
                if (string.IsNullOrEmpty(s_DataPath))
                {
                    const string path = PackagePath + "/Data";
                    if (!File.Exists(Path.GetFullPath(path)))
                    {
                        // if it's not a package, let's search through all assets
                        var apiDatabasePath = AssetDatabase.GetAllAssetPaths()
                            .FirstOrDefault(p => p.EndsWith("Data/ApiDatabase.json"));

                        if (string.IsNullOrEmpty(apiDatabasePath))
                            throw new Exception("Could not find ApiDatabase.json");
                        s_DataPath = apiDatabasePath.Substring(0, apiDatabasePath.IndexOf("/ApiDatabase.json"));
                    }
                }

                return s_DataPath;
            }
        }
        internal const string DefaultAssetPath = "Assets/Editor/ProjectAuditorConfig.asset";
        internal const string PackagePath = "Packages/com.unity.project-auditor";
        internal static string PackageVersion
        {
            get
            {
#if UNITY_2019_3_OR_NEWER
                var packageInfo = UnityEditor.PackageManager.PackageInfo.FindForAssetPath(PackagePath +  "/Editor/Unity.ProjectAuditor.Editor.asmdef");
                return packageInfo.version;
#else
                return "Unknown";
#endif
            }
        }
        internal static string ProjectPath
        {
            get
            {
                return PathUtils.GetDirectoryName(Application.dataPath);
            }
        }

        readonly List<ProjectAuditorModule> m_Modules = new List<ProjectAuditorModule>();
        ProjectAuditorConfig m_Config;

        public ProjectAuditorConfig config
        {
            get { return m_Config; }
        }

        public ProjectAuditor()
        {
            InitAsset(DefaultAssetPath);
            InitModules();
        }

        public ProjectAuditor(ProjectAuditorConfig projectAuditorConfig)
        {
            m_Config = projectAuditorConfig;
            InitModules();
        }

        /// <summary>
        /// ProjectAuditor constructor
        /// </summary>
        /// <param name="assetPath"> Path to the ProjectAuditorConfig asset</param>
        public ProjectAuditor(string assetPath)
        {
            InitAsset(assetPath);
            InitModules();
        }

        void InitAsset(string assetPath)
        {
            m_Config = AssetDatabase.LoadAssetAtPath<ProjectAuditorConfig>(assetPath);
            if (m_Config == null)
            {
                var path = Path.GetDirectoryName(assetPath);
                if (!File.Exists(path))
                    Directory.CreateDirectory(path);
                m_Config = ScriptableObject.CreateInstance<ProjectAuditorConfig>();
                AssetDatabase.CreateAsset(m_Config, assetPath);

                Debug.LogFormat("Project Auditor: {0} has been created.", assetPath);
            }
        }

        void InitModules()
        {
            foreach (var type in TypeCache.GetTypesDerivedFrom(typeof(ProjectAuditorModule)))
            {
                var instance = Activator.CreateInstance(type) as ProjectAuditorModule;
                instance.Initialize(m_Config);
                m_Modules.Add(instance);
            }
        }

        /// <summary>
        /// Runs all modules that are both supported and enabled.
        /// </summary>
        /// <param name="progress"> Progress bar, if applicable </param>
        /// <param name="projectAuditorParams"> Parameters to control the audit process </param>
        /// <returns> Generated report </returns>
        public ProjectReport Audit(ProjectAuditorParams projectAuditorParams, IProgress progress = null)
        {
            var task = AuditAsync(projectAuditorParams, progress);

            task.Wait();

            return task.Result;
        }

        public ProjectReport Audit(IProgress progress = null)
        {
            return Audit(new ProjectAuditorParams(), progress);
        }

        /// <summary>
        /// Runs all modules that are both supported and enabled.
        /// </summary>
        /// <param name="projectAuditorParams"> Parameters to control the audit process </param>
        /// <param name="progress"> Progress bar, if applicable </param>
        public Task<ProjectReport> AuditAsync(ProjectAuditorParams projectAuditorParams, IProgress progress = null)
        {
            var result = new ProjectReport();
            var requestedModules = projectAuditorParams.categories != null ? projectAuditorParams.categories.Select(GetModule).Distinct() : m_Modules.Where(m => m.IsEnabledByDefault());
            var supportedModules = requestedModules.Where(m => m.IsSupported()).ToArray();
            if (supportedModules.Length == 0)
            {
                // early out if, for any reason, there are no registered modules (return empty report)
                return Task.FromResult(result);
            }

            if (progress != null)
                progress.Start("Project Auditor", "Prepare Analysis", supportedModules.Length);

            var tasks = new List<Task>();
            var stopwatch = Stopwatch.StartNew();
            foreach (var module in supportedModules)
            {
                if (progress != null)
                    progress.Advance(module.GetType().Name + " Analysis");

                var startTime = stopwatch.ElapsedMilliseconds;
                var task = module.AuditAsync(new ProjectAuditorParams(projectAuditorParams), progress).ContinueWith(t =>
                {
                    if (m_Config.LogTimingsInfo)
                        Debug.Log(module.GetType().Name + " took: " +
                            (stopwatch.ElapsedMilliseconds - startTime) / 1000.0f + " seconds.");

                    if (projectAuditorParams.onModuleCompleted != null)
                    {
                        EditorApplication.delayCall += () =>
                        {
                            projectAuditorParams.onModuleCompleted(t.Result);
                        };
                    }

                    result.AddIssues(t.Result);
                });
                tasks.Add(task);
            }

            if (progress != null)
                progress.Clear();

            if (m_Config.LogTimingsInfo)
                Debug.Log("Project Auditor time to interactive: " + stopwatch.ElapsedMilliseconds / 1000.0f + " seconds.");

            return Task.WhenAll(tasks).ContinueWith((t) =>
            {
                stopwatch.Stop();
                if (m_Config.LogTimingsInfo)
                    Debug.Log("Project Auditor took: " + stopwatch.ElapsedMilliseconds / 1000.0f + " seconds.");

                return result;
            });
        }

        internal T GetModule<T>() where T : ProjectAuditorModule
        {
            foreach (var module in m_Modules)
            {
                if (module is T)
                    return (T)module;
            }

            return null;
        }

        internal ProjectAuditorModule GetModule(IssueCategory category)
        {
            return m_Modules.FirstOrDefault(a => a.IsSupported() && a.GetLayouts().FirstOrDefault(l => l.category == category) != null);
        }

        internal bool IsModuleSupported(IssueCategory category)
        {
            return m_Modules.Any(a => a.IsSupported() && a.GetLayouts().FirstOrDefault(l => l.category == category) != null);
        }

        public IssueLayout GetLayout(IssueCategory category)
        {
            var layouts = m_Modules.Where(a => a.IsSupported()).SelectMany(module => module.GetLayouts()).Where(l => l.category == category);
            return layouts.FirstOrDefault();
        }

        /// <summary>
        /// Get or Register a category by name. If the name argument does match an existing category, a new category is registered.
        /// </summary>
        /// <returns> Returns the category enum</returns>
        public static IssueCategory GetOrRegisterCategory(string name)
        {
            if (!s_CustomCategories.ContainsKey(name))
                s_CustomCategories.Add(name, IssueCategory.FirstCustomCategory + s_CustomCategories.Count);
            return s_CustomCategories[name];
        }

        /// <summary>
        /// Number of available built-in and registered categories
        /// </summary>
        /// <returns> Returns the number of available categories</returns>
        public static int NumCategories()
        {
            return (int)IssueCategory.FirstCustomCategory + s_CustomCategories.Count;
        }

#if UNITY_2018_1_OR_NEWER
        public int callbackOrder
        {
            get { return 0; }
        }

        public void OnPreprocessBuild(BuildReport report)
        {
            if (m_Config.AnalyzeOnBuild)
            {
                var projectReport = Audit();

                var numIssues = projectReport.NumTotalIssues;
                if (numIssues > 0)
                {
                    if (m_Config.FailBuildOnIssues)
                        Debug.LogError("Project Auditor found " + numIssues + " issues");
                    else
                        Debug.Log("Project Auditor found " + numIssues + " issues");
                }
            }
        }

#endif
    }
}
