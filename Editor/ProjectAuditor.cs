using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using Unity.ProjectAuditor.Editor.Core;
using Unity.ProjectAuditor.Editor.Diagnostic;
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

        public const string PackageName = "com.unity.project-auditor";
        public const string PackagePath = "Packages/" + PackageName;

        internal static string PackageVersion
        {
            get
            {
                if (string.IsNullOrEmpty(m_PackageVersion))
                    m_PackageVersion = PackageUtils.GetPackageVersion(PackageName);
                return m_PackageVersion;
            }
        }

        static string m_PackageVersion;

        readonly List<ProjectAuditorModule> m_Modules = new List<ProjectAuditorModule>();
        ProjectAuditorConfig m_Config;

        public ProjectAuditorConfig config => m_Config;

        readonly List<IProjectAuditorSettingsProvider> m_CustomSettingsProviders = new List<IProjectAuditorSettingsProvider>();
        internal ProjectAuditorPlatformSettingsProvider m_BuiltinSettingsProvider;

        // TODO: Once we register other providers, we could chose them instead of the built-in provider
        public IProjectAuditorSettingsProvider GetSettingsProvider() => m_BuiltinSettingsProvider;

        public ProjectAuditor()
        {
            InitSettingsProviders();
            InitAsset(DefaultAssetPath);
            InitModules();
        }

        public ProjectAuditor(ProjectAuditorConfig projectAuditorConfig)
        {
            InitSettingsProviders();
            m_Config = projectAuditorConfig;
            InitModules();
        }

        /// <summary>
        /// ProjectAuditor constructor
        /// </summary>
        /// <param name="assetPath"> Path to the ProjectAuditorConfig asset</param>
        public ProjectAuditor(string assetPath)
        {
            InitSettingsProviders();
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
                try
                {
                    instance.Initialize(m_Config);
                }
                catch (Exception e)
                {
                    Debug.LogError($"Project Auditor [{instance.name}]: " + e.Message);
                    continue;
                }
                m_Modules.Add(instance);
            }
        }

        void InitSettingsProviders()
        {
            InitBuiltinSettingsProvider();

            // TODO: Once we register other providers, we could chose them instead of the built-in provider
            foreach (var type in TypeCache.GetTypesDerivedFrom(typeof(IProjectAuditorSettingsProvider)))
            {
                // special type that is our default provider, a fallback to cover all platforms
                if (type == typeof(ProjectAuditorPlatformSettingsProvider))
                    continue;

                var instance = Activator.CreateInstance(type) as IProjectAuditorSettingsProvider;
                try
                {
                    instance.Initialize();
                }
                catch (Exception e)
                {
                    Debug.LogError($"Project Auditor couldn't create default settings provider: " + e.Message);
                    continue;
                }
                m_CustomSettingsProviders.Add(instance);
            }
        }

        void InitBuiltinSettingsProvider()
        {
            m_BuiltinSettingsProvider = new ProjectAuditorPlatformSettingsProvider();
            m_BuiltinSettingsProvider.Initialize();
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
            var requestedModules = projectAuditorParams.categories != null ? projectAuditorParams.categories.SelectMany(GetModules).Distinct() : m_Modules.Where(m => m.isEnabledByDefault).ToArray();
            var supportedModules = requestedModules.Where(m => m != null && m.isSupported && CoreUtils.SupportsPlatform(m.GetType(), projectAuditorParams.platform)).ToArray();
            var report = projectAuditorParams.existingReport;
            if (report == null)
                report = new ProjectReport();

            if (projectAuditorParams.categories != null)
            {
                foreach (var category in projectAuditorParams.categories)
                {
                    report.ClearIssues(category);
                }
            }

            var numModules = supportedModules.Length;
            if (numModules == 0)
            {
                // early out if, for any reason, there are no registered modules
                projectAuditorParams.onCompleted(report);
                return;
            }

            var logTimingsInfo = UserPreferences.logTimingsInfo;
            var stopwatch = Stopwatch.StartNew();
            foreach (var module in supportedModules)
            {
                var moduleStartTime = DateTime.Now;
                module.Audit(new ProjectAuditorParams(projectAuditorParams)
                {
                    onIncomingIssues = issues =>
                    {
                        report.AddIssues(issues);
                        projectAuditorParams.onIncomingIssues?.Invoke(issues);
                    },
                    onModuleCompleted = () =>
                    {
                        var moduleEndTime = DateTime.Now;
                        if (logTimingsInfo)
                            Debug.Log(module.name + " module took: " +
                                (moduleEndTime - moduleStartTime).TotalMilliseconds / 1000.0 + " seconds.");

                        report.RecordModuleInfo(module, moduleStartTime, moduleEndTime);

                        projectAuditorParams.onModuleCompleted?.Invoke();

                        var finished = --numModules == 0;
                        if (finished)
                        {
                            stopwatch.Stop();
                            if (logTimingsInfo)
                                Debug.Log("Project Auditor took: " + stopwatch.ElapsedMilliseconds / 1000.0f + " seconds.");

                            projectAuditorParams.onCompleted?.Invoke(report);
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

        internal ProjectAuditorModule[] GetModules(IssueCategory category)
        {
            return m_Modules.Where(a => a.isSupported && a.supportedLayouts.FirstOrDefault(l => l.category == category) != null).ToArray();
        }

        internal bool IsModuleSupported(IssueCategory category)
        {
            return m_Modules.Any(a => a.isSupported && a.supportedLayouts.FirstOrDefault(l => l.category == category) != null);
        }

        public IssueCategory[] GetCategories()
        {
            return m_Modules.Where(module => module.isSupported).SelectMany(m => m.categories).ToArray();
        }

        public IssueLayout GetLayout(IssueCategory category)
        {
            var layouts = m_Modules.Where(a => a.isSupported).SelectMany(module => module.supportedLayouts).Where(l => l.category == category);
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

        public static string GetCategoryName(IssueCategory category)
        {
            if (category < IssueCategory.FirstCustomCategory)
                return category.ToString();

            foreach (var pair in s_CustomCategories)
            {
                if (pair.Value == category)
                    return pair.Key;
            }

            return "Unknown";
        }

        /// <summary>
        /// Number of available built-in and registered categories
        /// </summary>
        /// <returns> Returns the number of available categories</returns>
        public static int NumCategories()
        {
            return (int)IssueCategory.FirstCustomCategory + s_CustomCategories.Count;
        }

        public int callbackOrder => 0;

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
