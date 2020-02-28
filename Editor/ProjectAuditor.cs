using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.ProjectAuditor.Editor.Auditors;
using UnityEditor;
using UnityEngine;
#if UNITY_2018_1_OR_NEWER
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

#endif

namespace Unity.ProjectAuditor.Editor
{
    public class ProjectAuditor
#if UNITY_2018_1_OR_NEWER
        : IPreprocessBuildWithReport
#endif
    {
        private static string m_DataPath;

        private string[] m_AuditorNames;
        private readonly List<IAuditor> m_Auditors = new List<IAuditor>();

        public ProjectAuditor()
        {
            const string path = "Assets/Editor";
            const string assetFilename = "ProjectAuditorConfig.asset";
            var assetPath = Path.Combine(path, assetFilename);
            config = AssetDatabase.LoadAssetAtPath<ProjectAuditorConfig>(assetPath);
            if (config == null)
            {
                if (!File.Exists(path)) Directory.CreateDirectory(path);
                config = ScriptableObject.CreateInstance<ProjectAuditorConfig>();
                AssetDatabase.CreateAsset(config, assetPath);
            }

            m_Auditors.Add(new ScriptAuditor(config));
            m_Auditors.Add(new SettingsAuditor(config));
            // Add more Auditors here...

            LoadDatabase();
        }

        public ProjectAuditorConfig config { get; set; }

        private static string dataPath
        {
            get
            {
                if (string.IsNullOrEmpty(m_DataPath))
                {
                    const string path = "Packages/com.unity.project-auditor/Data";
                    if (!File.Exists(Path.GetFullPath(path)))
                    {
                        // if it's not a package, let's search through all assets
                        var apiDatabasePath = AssetDatabase.GetAllAssetPaths()
                            .FirstOrDefault(p => p.EndsWith("Data/ApiDatabase.json"));

                        if (string.IsNullOrEmpty(apiDatabasePath))
                            throw new Exception("Could not find ApiDatabase.json");
                        m_DataPath = apiDatabasePath.Substring(0, apiDatabasePath.IndexOf("/ApiDatabase.json"));
                    }
                }

                return m_DataPath;
            }
        }

        public ProjectReport Audit(IProgressBar progressBar = null)
        {
            var projectReport = new ProjectReport();
            foreach (var auditor in m_Auditors) auditor.Audit(projectReport, progressBar);

            return projectReport;
        }

        public T GetAuditor<T>() where T : class
        {
            foreach (var iauditor in m_Auditors)
            {
                var auditor = iauditor as T;
                if (auditor != null)
                    return auditor;
            }

            return null;
        }

        public void LoadDatabase(string path)
        {
            foreach (var auditor in m_Auditors) auditor.LoadDatabase(path);
        }

        public void LoadDatabase()
        {
            LoadDatabase(dataPath);
        }

#if UNITY_2018_1_OR_NEWER
        public int callbackOrder { get; }
        public void OnPreprocessBuild(BuildReport report)
        {
            if (config.enableAnalyzeOnBuild)
            {
                var projectReport = Audit();

                var numIssues = projectReport.NumTotalIssues;
                if (numIssues > 0)
                {
                    if (config.enableFailBuildOnIssues)
                        Debug.LogError("Project Auditor found " + numIssues + " issues");
                    else
                        Debug.Log("Project Auditor found " + numIssues + " issues");
                }
            }
        }
#endif
    }
}