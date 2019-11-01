using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Unity.ProjectAuditor.Editor
{
    public class ProjectAuditor : IAuditor, IPreprocessBuildWithReport
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
        
        public string[] auditorNames
        {
            get
            {
                if (m_AuditorNames != null)
                    return m_AuditorNames;

                List<string> names = new List<string>();
                foreach (var auditor in m_Auditors)
                {
                    names.Add(auditor.GetUIName());                    
                }

                m_AuditorNames = names.ToArray();
                return m_AuditorNames;
            }
        }

        private static string m_DataPath;

        public static string dataPath
        {
            get
            {
                if (string.IsNullOrEmpty(m_DataPath))
                {
                    var path = "Packages/com.unity.project-auditor/Data";
                    if (!File.Exists(Path.GetFullPath(path)))
                    {
                        // if it's not a package, let's search through all assets
                        string apiDatabasePath = AssetDatabase.GetAllAssetPaths()
                            .Where(p => p.EndsWith("Data/ApiDatabase.json")).FirstOrDefault();

                        if (string.IsNullOrEmpty(apiDatabasePath))
                            throw new Exception("Could not find ApiDatabase.json");
                        m_DataPath = apiDatabasePath.Substring(0, apiDatabasePath.IndexOf("/ApiDatabase.json"));
                    }
                }

                return m_DataPath;
            }
        }

        public string GetUIName()
        {
            return "Project Auditor";
        }
      
        public ProjectAuditor()
        {
            var path = "Assets/Editor"; 
            var assetFilename = "ProjectAuditorConfig.asset";
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

        public void Audit(ProjectReport projectReport)
        {
            foreach (var auditor in m_Auditors)
            {
                auditor.Audit(projectReport);
            }

            EditorUtility.ClearProgressBar();
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

        public int callbackOrder { get; }
        public void OnPreprocessBuild(BuildReport report)
        {
            if (m_ProjectAuditorConfig.enableAnalyzeOnBuild)
            {
                var projectReport = new ProjectReport();
                Audit(projectReport);

                var numIssues = projectReport.NumIssues;
                if (numIssues > 0)
                {
                    if (m_ProjectAuditorConfig.enableFailBuildOnIssues)
                        Debug.LogError("Project Auditor found " + numIssues + " issues");
                    else
                        Debug.Log("Project Auditor found " + numIssues + " issues");
                }
                    
            }            
        }
    }
}