using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;

namespace Unity.ProjectAuditor.Editor
{
    public class ProjectAuditor : IAuditor
    {
        private List<IAuditor> m_Auditors = new List<IAuditor>();

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
        
        public ProjectAuditor()
        {
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
    }
}