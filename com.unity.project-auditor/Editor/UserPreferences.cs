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

        static readonly string k_AnalyzeAfterBuildLabel = "Auto Analyze after Build";
        static readonly bool k_AnalyzeAfterBuildDefault = false;

        static readonly string k_AnalysisInBackgroundLabel = "Analyze in Background";
        static readonly bool k_AnalysisInBackgroundDefault = true;

        static readonly string k_UseRoslynAnalyzersLabel = "Use Roslyn Analyzers";
        static readonly bool k_UseRoslynAnalyzersDefault = false;

        static readonly string k_FailBuildOnIssuesLabel = "Fail Build on Issues";
        static readonly bool k_FailBuildOnIssuesDefault = false;

        static readonly string k_LogTimingsInfoLabel = "Log timing information";

        static readonly string k_PrettifyJSONOutputLabel = "Prettify saved JSON files";

        static readonly string k_DeveloperModeLabel = "Enable Developer Mode";

        internal static string LoadSavePath = string.Empty;

        public static string Path => k_PreferencesKey;

        /// <summary>
        /// If enabled, ProjectAuditor will re-run the BuildReport analysis every time the project is built.
        /// </summary>
        public static bool AnalyzeAfterBuild
        {
            get => EditorPrefs.GetBool(MakeKey(nameof(AnalyzeAfterBuild)), k_AnalyzeAfterBuildDefault);
            set => EditorPrefs.SetBool(MakeKey(nameof(AnalyzeAfterBuild)), value);
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
                AssetDatabase.ImportAsset(ProjectAuditorPackage.Path + "/Editor/UserPreferences.cs");
            }

            PrettifyJsonOutput = EditorGUILayout.Toggle(k_PrettifyJSONOutputLabel, PrettifyJsonOutput);

            EditorGUILayout.LabelField("Analysis", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            AnalyzeInBackground = EditorGUILayout.Toggle(k_AnalysisInBackgroundLabel, AnalyzeInBackground);
            UseRoslynAnalyzers = EditorGUILayout.Toggle(k_UseRoslynAnalyzersLabel, UseRoslynAnalyzers);
            LogTimingsInfo = EditorGUILayout.Toggle(k_LogTimingsInfoLabel, LogTimingsInfo);

            EditorGUI.indentLevel--;

            GUILayout.Space(10f);

            EditorGUILayout.LabelField("Build", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            AnalyzeAfterBuild = EditorGUILayout.Toggle(k_AnalyzeAfterBuildLabel, AnalyzeAfterBuild);

            FailBuildOnIssues = EditorGUILayout.Toggle(k_FailBuildOnIssuesLabel, FailBuildOnIssues);

            EditorGUI.indentLevel--;
            EditorGUI.indentLevel--;
        }
    }
}
