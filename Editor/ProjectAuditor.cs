using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        private List<IAuditor> m_Auditors = new List<IAuditor>();
        private ProjectAuditorConfig m_ProjectAuditorConfig;

        public ProjectAuditorConfig config
        {
            get
            {
                return m_ProjectAuditorConfig;
            }
        }
        
        private string[] m_AuditorNames;
        
        private static string m_DataPath;

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
                        string apiDatabasePath = AssetDatabase.GetAllAssetPaths().FirstOrDefault(p => p.EndsWith("Data/ApiDatabase.json"));

                        if (string.IsNullOrEmpty(apiDatabasePath))
                            throw new Exception("Could not find ApiDatabase.json");
                        m_DataPath = apiDatabasePath.Substring(0, apiDatabasePath.IndexOf("/ApiDatabase.json"));
                    }
                }

                return m_DataPath;
            }
        }

        public ProjectAuditor()
        {
            const string path = "Assets/Editor"; 
            const string assetFilename = "ProjectAuditorConfig.asset";
            var assetPath = Path.Combine(path, assetFilename);
            m_ProjectAuditorConfig = AssetDatabase.LoadAssetAtPath<ProjectAuditorConfig>(assetPath);
            if (m_ProjectAuditorConfig == null)
            {
                if (!File.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }
                m_ProjectAuditorConfig = ScriptableObject.CreateInstance<ProjectAuditorConfig>();
                AssetDatabase.CreateAsset(m_ProjectAuditorConfig, assetPath);
            }
            
            m_Auditors.Add(new ScriptAuditor());
            m_Auditors.Add(new SettingsAuditor());
            // Add more Auditors here...

            LoadDatabase();
        }
        public ProjectReport Audit(IProgressBar progressBar = null)
        {
            var projectReport = new ProjectReport(); 
            foreach (var auditor in m_Auditors)
            {
                auditor.Audit(projectReport, progressBar);
            }

            return projectReport;
        }

        public T GetAuditor<T>() where T: class
        {
            foreach (var iauditor in m_Auditors)
            {
                T auditor = iauditor as T;
                if (auditor != null)
                    return auditor;
            }

            return null;
        }

        public void LoadDatabase(string path)
        {
            foreach (var auditor in m_Auditors)
            {
                auditor.LoadDatabase(path);
            }
        }
        
        public void LoadDatabase()
        {
            LoadDatabase(dataPath);
        }

#if UNITY_2018_1_OR_NEWER
        public int callbackOrder { get; }
        public void OnPreprocessBuild(BuildReport report)
        {
            if (m_ProjectAuditorConfig.enableAnalyzeOnBuild)
            {
                var projectReport = Audit();

                var numIssues = projectReport.NumTotalIssues;
                if (numIssues > 0)
                {
                    if (m_ProjectAuditorConfig.enableFailBuildOnIssues)
                        Debug.LogError("Project Auditor found " + numIssues + " issues");
                    else
                        Debug.Log("Project Auditor found " + numIssues + " issues");
                } 
            }            
        }
#endif
    }
}
