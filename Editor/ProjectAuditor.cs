using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
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
        static Dictionary<string, IssueCategory> s_CustomCategories = new Dictionary<string, IssueCategory>();
        static string s_DataPath;

        public static string DataPath
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
        public const string DefaultAssetPath = "Assets/Editor/ProjectAuditorConfig.asset";
        public const string PackagePath = "Packages/com.unity.project-auditor";

        public static string PackageVersion
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
        /// Runs all available modules (code, project settings) and generate a report of all found issues.
        /// </summary>
        /// <param name="progress"> Progress bar, if applicable </param>
        /// <returns> Generated report </returns>
        public ProjectReport Audit(IProgress progress = null)
        {
            var projectReport = new ProjectReport();
            var completed = false;

            Audit(projectReport.AddIssue, _completed => { completed = _completed; }, progress);

            while (!completed)
                Thread.Sleep(50);
            return projectReport;
        }

        /// <summary>
        /// Runs all available modules (code, project settings) and generate a report of all found issues.
        /// </summary>
        /// <param name="onIssueFound"> Action called whenever a new issue is found </param>
        /// <param name="onUpdate"> Action called whenever a module completes </param>
        /// <param name="progress"> Progress bar, if applicable </param>
        public void Audit(Action<ProjectIssue> onIssueFound, Action<bool> onUpdate, IProgress progress = null)
        {
            var numModules = m_Modules.Count;
            if (numModules == 0)
            {
                // early out if, for any reason, there are no registered modules
                onUpdate(true);
                return;
            }

            var stopwatch = Stopwatch.StartNew();
            foreach (var module in m_Modules)
            {
                var startTime = stopwatch.ElapsedMilliseconds;
                module.Audit(onIssueFound, () =>
                {
                    if (m_Config.LogTimingsInfo)
                        Debug.Log(module.GetType().Name + " took: " + (stopwatch.ElapsedMilliseconds - startTime) / 1000.0f + " seconds.");

                    var finished = --numModules == 0;
                    if (finished)
                    {
                        stopwatch.Stop();
                        if (m_Config.LogTimingsInfo)
                            Debug.Log("Project Auditor took: " + stopwatch.ElapsedMilliseconds / 1000.0f + " seconds.");
                    }

                    onUpdate(finished);
                }, progress);
            }

            if (m_Config.LogTimingsInfo)
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

        public IssueLayout GetLayout(IssueCategory category)
        {
            var layouts = m_Modules.Where(a => a.IsSupported()).SelectMany(module => module.GetLayouts()).Where(l => l.category == category);
            return layouts.FirstOrDefault();
        }

        public static IssueCategory GetOrRegisterCategory(string name)
        {
            if (!s_CustomCategories.ContainsKey(name))
                s_CustomCategories.Add(name, IssueCategory.FirstCustomCategory + s_CustomCategories.Count);
            return s_CustomCategories[name];
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
