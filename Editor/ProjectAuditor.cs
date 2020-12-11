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
        static string s_DataPath;

        readonly List<IAuditor> m_Auditors = new List<IAuditor>();
        ProjectAuditorConfig m_Config;

        public const string DefaultAssetPath = "Assets/Editor/ProjectAuditorConfig.asset";

        public ProjectAuditorConfig config
        {
            get { return m_Config; }
        }

        public ProjectAuditor()
        {
            InitAsset(DefaultAssetPath);
            InitAuditors();
        }

        public ProjectAuditor(ProjectAuditorConfig projectAuditorConfig)
        {
            m_Config = projectAuditorConfig;
            InitAuditors();
        }

        /// <summary>
        /// ProjectAuditor constructor
        /// </summary>
        /// <param name="assetPath"> Path to the ProjectAuditorConfig asset</param>
        public ProjectAuditor(string assetPath)
        {
            InitAsset(assetPath);
            InitAuditors();
        }

        void InitAsset(string assetPath)
        {
            m_Config = AssetDatabase.LoadAssetAtPath<ProjectAuditorConfig>(assetPath);
            if (m_Config == null)
            {
                Debug.LogWarningFormat("Project Auditor: {0} not found.", assetPath);

                var path = Path.GetDirectoryName(assetPath);
                if (!File.Exists(path))
                    Directory.CreateDirectory(path);
                m_Config = ScriptableObject.CreateInstance<ProjectAuditorConfig>();
                AssetDatabase.CreateAsset(m_Config, assetPath);

                Debug.LogFormat("Project Auditor: {0} has been created.", assetPath);
            }
        }

        void InitAuditors()
        {
            foreach (var type in AssemblyHelper.GetAllTypesInheritedFromInterface<IAuditor>())
            {
                var instance = Activator.CreateInstance(type) as IAuditor;
                instance.Initialize(m_Config);
                instance.Reload(DataPath);
                m_Auditors.Add(instance);
            }
        }

        public static string DataPath
        {
            get
            {
                if (string.IsNullOrEmpty(s_DataPath))
                {
                    const string path = "Packages/com.unity.project-auditor/Data";
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

        /// <summary>
        /// Runs all available auditors (code, project settings) and generate a report of all found issues.
        /// </summary>
        /// <param name="progressBar"> Progress bar, if applicable </param>
        /// <returns> Generated report </returns>
        public ProjectReport Audit(IProgressBar progressBar = null)
        {
            var projectReport = new ProjectReport();
            var completed = false;

            Audit(projectReport.AddIssue, _completed => { completed = _completed; }, progressBar);

            while (!completed)
                Thread.Sleep(50);
            return projectReport;
        }

        /// <summary>
        /// Runs all available auditors (code, project settings) and generate a report of all found issues.
        /// </summary>
        /// <param name="onIssueFound"> Action called whenever a new issue is found </param>
        /// <param name="onUpdate"> Action called whenever an internal auditor completes </param>
        /// <param name="progressBar"> Progress bar, if applicable </param>
        public void Audit(Action<ProjectIssue> onIssueFound, Action<bool> onUpdate, IProgressBar progressBar = null)
        {
            var numAuditors = m_Auditors.Count;
            if (numAuditors == 0)
            {
                // early out if, for any reason, there are no registered Auditors
                onUpdate(true);
                return;
            }

            var stopwatch = Stopwatch.StartNew();
            foreach (var auditor in m_Auditors)
            {
                var startTime = stopwatch.ElapsedMilliseconds;
                auditor.Audit(onIssueFound, () =>
                {
                    if (m_Config.LogTimingsInfo)
                        Debug.Log(auditor.GetType().Name + " took: " + (stopwatch.ElapsedMilliseconds - startTime) / 1000.0f + " seconds.");

                    var finished = --numAuditors == 0;
                    if (finished)
                    {
                        stopwatch.Stop();
                        if (m_Config.LogTimingsInfo)
                            Debug.Log("Project Auditor took: " + stopwatch.ElapsedMilliseconds / 1000.0f + " seconds.");
                    }

                    onUpdate(finished);
                }, progressBar);
            }

            Debug.Log("Project Auditor time to interactive: " + stopwatch.ElapsedMilliseconds / 1000.0f + " seconds.");
        }

        internal T GetAuditor<T>() where T : class
        {
            foreach (var iauditor in m_Auditors)
            {
                var auditor = iauditor as T;
                if (auditor != null)
                    return auditor;
            }

            return null;
        }

        public void Reload(string path)
        {
            foreach (var auditor in m_Auditors) auditor.Reload(path);
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
