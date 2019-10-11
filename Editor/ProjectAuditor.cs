using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Mono.Cecil;
using Mono.Cecil.Cil;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;
using UnityEditor.Macros;

namespace Unity.ProjectAuditor.Editor
{
    public class ProjectAuditor
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

            LoadDatabase();
        }

        public ProjectReport Audit()
        {
            var projectReport = new ProjectReport();

            foreach (var auditor in m_Auditors)
            {
                auditor.Audit(projectReport);
            }

            EditorUtility.ClearProgressBar();

            return projectReport;
        }

        public void LoadDatabase()
        {
            foreach (var auditor in m_Auditors)
            {
                auditor.LoadDatabase(dataPath);
            }
        }
    }
}