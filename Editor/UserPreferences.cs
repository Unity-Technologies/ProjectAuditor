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

        static readonly string k_PrettifyJSONOutputLabel = "Prettify saved JSON files";

        static readonly string k_DeveloperModeLabel = "Enable Developer Mode";

        static string k_BuildReportAutoSaveLabel = "Auto Save Last Report";
        static bool k_BuildReportAutoSaveDefault = false;

        static string k_BuildReportPathLabel = "Library Path";
        static string k_BuildReportPathDefault = "Assets/BuildReports";

        static string k_RulesPathLabel = "Rules Asset Path";
        static string k_RulesPathDefault = "Assets/Editor/ProjectAuditorRules.asset";

        internal static string LoadSavePath = string.Empty;

        public static string Path => k_PreferencesKey;

        /// <summary>
        /// If enabled, ProjectAuditor will run every time the project is built.
        /// </summary>
        public static bool AnalyzeOnBuild
        {
            get => EditorPrefs.GetBool(MakeKey(nameof(AnalyzeOnBuild)), k_AnalysisOnBuildDefault);
            set => EditorPrefs.SetBool(MakeKey(nameof(AnalyzeOnBuild)), value);
        }

        /// <summary>
        /// If enabled, ProjectAuditor will try to partially analyze the project in the background.
        /// </summary>
        public static bool AnalyzeInBackground
        {
            get => EditorPrefs.GetBool(MakeKey(nameof(AnalyzeInBackground)), k_AnalysisInBackgroundDefault);
            set => EditorPrefs.SetBool(MakeKey(nameof(AnalyzeInBackground)), value);
        }

        /// <summary>
        /// If enabled, ProjectAuditor will use Roslyn Analyzer DLLs that are present in the project
        /// </summary>
        public static bool UseRoslynAnalyzers
        {
            get => EditorPrefs.GetBool(MakeKey(nameof(UseRoslynAnalyzers)), k_UseRoslynAnalyzersDefault);
            set => EditorPrefs.SetBool(MakeKey(nameof(UseRoslynAnalyzers)), value);
        }

        /// <summary>
        /// If enabled, any issue reported by ProjectAuditor will cause the build to fail.
        /// </summary>
        public static bool FailBuildOnIssues
        {
            get => EditorPrefs.GetBool(MakeKey(nameof(FailBuildOnIssues)), k_FailBuildOnIssuesDefault);
            set => EditorPrefs.SetBool(MakeKey(nameof(FailBuildOnIssues)), value);
        }

        /// <summary>
        /// If enabled, the BuildReport is automatically saved as asset after each build
        /// </summary>
        public static bool BuildReportAutoSave
        {
            get => EditorPrefs.GetBool(MakeKey(nameof(BuildReportAutoSave)), k_BuildReportAutoSaveDefault);
            set => EditorPrefs.SetBool(MakeKey(nameof(BuildReportAutoSave)), value);
        }

        /// <summary>
        /// Customizable path to save the BuildReport
        /// </summary>
        public static string BuildReportPath
        {
            get => EditorPrefs.GetString(MakeKey(nameof(BuildReportPath)), k_BuildReportPathDefault);
            set => EditorPrefs.SetString(MakeKey(nameof(BuildReportPath)), value);
        }

        public static bool DeveloperMode
        {
            get => EditorPrefs.GetBool(MakeKey(nameof(DeveloperMode)), false);
            set => EditorPrefs.SetBool(MakeKey(nameof(DeveloperMode)), value);
        }

        public static bool PrettifyJsonOutput
        {
            get => EditorPrefs.GetBool(MakeKey(nameof(PrettifyJsonOutput)), false);
            set => EditorPrefs.SetBool(MakeKey(nameof(PrettifyJsonOutput)), value);
        }

        public static bool LogTimingsInfo
        {
            get => EditorPrefs.GetBool(MakeKey(nameof(LogTimingsInfo)), false);
            set => EditorPrefs.SetBool(MakeKey(nameof(LogTimingsInfo)), value);
        }

        public static string SettingsAsset
        {
            get => EditorPrefs.GetString(MakeKey(nameof(SettingsAsset)), "");
            set => EditorPrefs.SetString(MakeKey(nameof(SettingsAsset)), value);
        }

        public static string RulesAssetPath
        {
            get => EditorPrefs.GetString(MakeKey(nameof(RulesAssetPath)), k_RulesPathDefault);
            set => EditorPrefs.SetString(MakeKey(nameof(RulesAssetPath)), value);
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

            var value = EditorGUILayout.Toggle(k_DeveloperModeLabel, DeveloperMode);
            if (value != DeveloperMode)
            {
                DeveloperMode = value;

                // need to trigger domain reload so that Views are re-registered
                AssetDatabase.ImportAsset(ProjectAuditor.PackagePath + "/Editor/UserPreferences.cs");
            }

            PrettifyJsonOutput = EditorGUILayout.Toggle(k_PrettifyJSONOutputLabel, PrettifyJsonOutput);

            EditorGUILayout.LabelField("Analysis", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            AnalyzeInBackground = EditorGUILayout.Toggle(k_AnalysisInBackgroundLabel, AnalyzeInBackground);
            UseRoslynAnalyzers = EditorGUILayout.Toggle(k_UseRoslynAnalyzersLabel, UseRoslynAnalyzers);
            LogTimingsInfo = EditorGUILayout.Toggle(k_LogTimingsInfoLabel, LogTimingsInfo);

            EditorGUILayout.BeginHorizontal();
            var newRulesPath = EditorGUILayout.DelayedTextField(k_RulesPathLabel, RulesAssetPath);
            if (!string.IsNullOrEmpty(newRulesPath))
                RulesAssetPath = newRulesPath;
            if (GUILayout.Button("Browse...", GUILayout.Width(80)))
            {
                newRulesPath = EditorUtility.OpenFilePanel("Select Project Auditor Rules asset path/filename", RulesAssetPath, "asset");
                if (!string.IsNullOrEmpty(newRulesPath))
                {
                    RulesAssetPath = FileUtil.GetProjectRelativePath(newRulesPath);
                    InternalEditorUtility.RepaintAllViews();
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUI.indentLevel--;

            GUILayout.Space(10f);

            EditorGUILayout.LabelField("Build", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            AnalyzeOnBuild = EditorGUILayout.Toggle(k_AnalysisOnBuildLabel, AnalyzeOnBuild);
            BuildReportAutoSave = EditorGUILayout.Toggle(k_BuildReportAutoSaveLabel, BuildReportAutoSave);

            GUI.enabled = BuildReportAutoSave;

            EditorGUILayout.BeginHorizontal();
            var newPath = EditorGUILayout.DelayedTextField(k_BuildReportPathLabel, BuildReportPath);
            if (!string.IsNullOrEmpty(newPath))
                BuildReportPath = newPath;
            if (GUILayout.Button("Browse...", GUILayout.Width(80)))
            {
                newPath = EditorUtility.OpenFolderPanel("Select Build Report destination", BuildReportPath, "");
                if (!string.IsNullOrEmpty(newPath))
                {
                    BuildReportPath = FileUtil.GetProjectRelativePath(newPath);
                    InternalEditorUtility.RepaintAllViews();
                }
            }
            EditorGUILayout.EndHorizontal();

            GUI.enabled = true;

            FailBuildOnIssues = EditorGUILayout.Toggle(k_FailBuildOnIssuesLabel, FailBuildOnIssues);

            EditorGUI.indentLevel--;
            EditorGUI.indentLevel--;
        }
    }
}
