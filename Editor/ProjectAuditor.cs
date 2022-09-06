using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using Unity.ProjectAuditor.Editor.Core;
using Unity.ProjectAuditor.Editor.Utils;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace Unity.ProjectAuditor.Editor
{
    /// <summary>
    /// ProjectAuditor class is responsible for auditing the Unity project
    /// </summary>
    public sealed class ProjectAuditor
        : IPreprocessBuildWithReport
    {
        static readonly Dictionary<string, IssueCategory> s_CustomCategories = new Dictionary<string, IssueCategory>();

        internal const string DataPath = PackagePath + "/Data";
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
            ProjectReport projectReport = null;

            projectAuditorParams.onCompleted += result => { projectReport = result; };

            AuditAsync(projectAuditorParams, progress);

            while (projectReport == null)
                Thread.Sleep(50);
            return projectReport;
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
        public void AuditAsync(ProjectAuditorParams projectAuditorParams, IProgress progress = null)
        {
            var result = new ProjectReport();
            var requestedModules = projectAuditorParams.categories != null ? projectAuditorParams.categories.Select(GetModule).Distinct() : m_Modules.Where(m => m.IsEnabledByDefault());
            var supportedModules = requestedModules.Where(m => m != null && m.IsSupported()).ToArray();
            var numModules = supportedModules.Length;
            if (numModules == 0)
            {
                // early out if, for any reason, there are no registered modules
                projectAuditorParams.onCompleted(result);
                return;
            }

            var logTimingsInfo = UserPreferences.logTimingsInfo;
            var stopwatch = Stopwatch.StartNew();
            foreach (var module in supportedModules)
            {
                var startTime = stopwatch.ElapsedMilliseconds;
                module.Audit(new ProjectAuditorParams(projectAuditorParams)
                {
                    onIncomingIssues = issues =>
                    {
                        result.AddIssues(issues);
                        projectAuditorParams.onIncomingIssues?.Invoke(issues);
                    },
                    onModuleCompleted = () =>
                    {
                        if (logTimingsInfo)
                            Debug.Log(module.GetType().Name + " took: " +
                                (stopwatch.ElapsedMilliseconds - startTime) / 1000.0f + " seconds.");

                        projectAuditorParams.onModuleCompleted?.Invoke();

                        var finished = --numModules == 0;
                        if (finished)
                        {
                            stopwatch.Stop();
                            if (logTimingsInfo)
                                Debug.Log("Project Auditor took: " + stopwatch.ElapsedMilliseconds / 1000.0f + " seconds.");

                            projectAuditorParams.onCompleted?.Invoke(result);
                        }
                    }
                }, progress);
            }

            if (logTimingsInfo)
                Debug.Log("Project Auditor time to interactive: " + stopwatch.ElapsedMilliseconds / 1000.0f + " seconds.");
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

        public IssueCategory[] GetCategories()
        {
            return m_Modules.Where(module => module.IsSupported()).SelectMany(m => m.GetCategories()).ToArray();
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
    }
}
