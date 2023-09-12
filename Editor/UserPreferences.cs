using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Unity.ProjectAuditor.Editor
{
    internal class UserPreferences
    {
        static readonly string k_PreferencesKey = "Preferences/Analysis/Project Auditor";

        static readonly string k_EditorPrefsPrefix = "ProjectAuditor";

        static readonly string k_AnalysisOnBuildLabel = "Auto Analysis on Build";
        static readonly bool k_AnalysisOnBuildDefault = false;

        static readonly string k_AnalysisInBackgroundLabel = "Analysis in Background";
        static readonly bool k_AnalysisInBackgroundDefault = true;

        static readonly string k_UseRoslynAnalyzersLabel = "Use Roslyn Analyzers";
        static readonly bool k_UseRoslynAnalyzersDefault = false;

        static readonly string k_FailBuildOnIssuesLabel = "Fail Build on Issues";
        static readonly bool k_FailBuildOnIssuesDefault = false;

        static readonly string k_LogTimingsInfoLabel = "Log timing information";

        static readonly string k_DeveloperModeLabel = "Enable Developer Mode";

        static string k_BuildReportAutoSaveLabel = "Auto Save Last Report";
        static bool k_BuildReportAutoSaveDefault = false;

        static string k_BuildReportPathLabel = "Library Path";
        static string k_BuildReportPathDefault = "Assets/BuildReports";

        internal static string loadSavePath = string.Empty;

        public static string Path => k_PreferencesKey;

        /// <summary>
        /// If enabled, ProjectAuditor will run every time the project is built.
        /// </summary>
        public static bool analyzeOnBuild
        {
            get => EditorPrefs.GetBool(MakeKey(nameof(analyzeOnBuild)), k_AnalysisOnBuildDefault);
            set => EditorPrefs.SetBool(MakeKey(nameof(analyzeOnBuild)), value);
        }

        /// <summary>
        /// If enabled, ProjectAuditor will try to partially analyze the project in the background.
        /// </summary>
        public static bool analyzeInBackground
        {
            get => EditorPrefs.GetBool(MakeKey(nameof(analyzeInBackground)), k_AnalysisInBackgroundDefault);
            set => EditorPrefs.SetBool(MakeKey(nameof(analyzeInBackground)), value);
        }

        /// <summary>
        /// If enabled, ProjectAuditor will use Roslyn Analyzer DLLs that are present in the project
        /// </summary>
        public static bool useRoslynAnalyzers
        {
            get => EditorPrefs.GetBool(MakeKey(nameof(useRoslynAnalyzers)), k_UseRoslynAnalyzersDefault);
            set => EditorPrefs.SetBool(MakeKey(nameof(useRoslynAnalyzers)), value);
        }

        /// <summary>
        /// If enabled, any issue reported by ProjectAuditor will cause the build to fail.
        /// </summary>
        public static bool failBuildOnIssues
        {
            get => EditorPrefs.GetBool(MakeKey(nameof(failBuildOnIssues)), k_FailBuildOnIssuesDefault);
            set => EditorPrefs.SetBool(MakeKey(nameof(failBuildOnIssues)), value);
        }

        /// <summary>
        /// If enabled, the BuildReport is automatically saved as asset after each build
        /// </summary>
        public static bool buildReportAutoSave
        {
            get => EditorPrefs.GetBool(MakeKey(nameof(buildReportAutoSave)), k_BuildReportAutoSaveDefault);
            set => EditorPrefs.SetBool(MakeKey(nameof(buildReportAutoSave)), value);
        }

        /// <summary>
        /// Customizable path to save the BuildReport
        /// </summary>
        public static string buildReportPath
        {
            get => EditorPrefs.GetString(MakeKey(nameof(buildReportPath)), k_BuildReportPathDefault);
            set => EditorPrefs.SetString(MakeKey(nameof(buildReportPath)), value);
        }

        public static bool developerMode
        {
            get => EditorPrefs.GetBool(MakeKey(nameof(developerMode)), false);
            set => EditorPrefs.SetBool(MakeKey(nameof(developerMode)), value);
        }

        public static bool logTimingsInfo
        {
            get => EditorPrefs.GetBool(MakeKey(nameof(logTimingsInfo)), false);
            set => EditorPrefs.SetBool(MakeKey(nameof(logTimingsInfo)), value);
        }

        public static string settingsAsset
        {
            get => EditorPrefs.GetString(MakeKey(nameof(settingsAsset)), "");
            set => EditorPrefs.SetString(MakeKey(nameof(settingsAsset)), value);
        }

        [SettingsProvider]
        internal static SettingsProvider CreatePreferencesProvider()
        {
            var settings = new SettingsProvider(k_PreferencesKey, SettingsScope.User)
            {
                guiHandler = PreferencesGUI,
                keywords = new HashSet<string>(new[] { "performance", "static", "analysis" })
            };
            return settings;
        }

        static string MakeKey(string key)
        {
            return $"{k_EditorPrefsPrefix}.{key}";
        }

        static void PreferencesGUI(string searchContext)
        {
            const float labelWidth = 300f;

            EditorGUIUtility.labelWidth = labelWidth;

            EditorGUI.indentLevel++;

            var value = EditorGUILayout.Toggle(k_DeveloperModeLabel, developerMode);
            if (value != developerMode)
            {
                developerMode = value;

                // need to trigger domain reload so that Views are re-registered
                AssetDatabase.ImportAsset(ProjectAuditor.s_PackagePath + "/Editor/UserPreferences.cs");
            }

            EditorGUILayout.LabelField("Analysis", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            analyzeInBackground = EditorGUILayout.Toggle(k_AnalysisInBackgroundLabel, analyzeInBackground);
            useRoslynAnalyzers = EditorGUILayout.Toggle(k_UseRoslynAnalyzersLabel, useRoslynAnalyzers);
            logTimingsInfo = EditorGUILayout.Toggle(k_LogTimingsInfoLabel, logTimingsInfo);
            EditorGUI.indentLevel--;

            GUILayout.Space(10f);

            EditorGUILayout.LabelField("Build", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            analyzeOnBuild = EditorGUILayout.Toggle(k_AnalysisOnBuildLabel, analyzeOnBuild);
            buildReportAutoSave = EditorGUILayout.Toggle(k_BuildReportAutoSaveLabel, buildReportAutoSave);

            GUI.enabled = buildReportAutoSave;

            EditorGUILayout.BeginHorizontal();
            var newPath = EditorGUILayout.DelayedTextField(k_BuildReportPathLabel, buildReportPath);
            if (!string.IsNullOrEmpty(newPath))
                buildReportPath = newPath;
            if (GUILayout.Button("Browse...", GUILayout.Width(80)))
            {
                newPath = EditorUtility.OpenFolderPanel("Select Build Report destination", buildReportPath, "");
                if (!string.IsNullOrEmpty(newPath))
                {
                    buildReportPath = FileUtil.GetProjectRelativePath(newPath);
                    InternalEditorUtility.RepaintAllViews();
                }
            }
            EditorGUILayout.EndHorizontal();

            GUI.enabled = true;

            failBuildOnIssues = EditorGUILayout.Toggle(k_FailBuildOnIssuesLabel, failBuildOnIssues);

            EditorGUI.indentLevel--;
            EditorGUI.indentLevel--;
        }
    }
}
