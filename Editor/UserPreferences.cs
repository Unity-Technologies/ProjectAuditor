using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Unity.ProjectAuditor.Editor
{
    public class UserPreferences
    {
        static readonly string k_PreferencesKey = "Preferences/Analysis/Project Auditor";

        static readonly string k_EditorPrefsPrefix = "ProjectAuditor";
        static readonly string k_LogTimingsInfoKey = k_EditorPrefsPrefix + ".logTimingsInfo";
        static readonly string k_LogTimingsInfoLabel = "Log timing information";

        static readonly string k_DeveloperModeKey = k_EditorPrefsPrefix + ".developerMode";
        static readonly string k_DeveloperModeLabel = "Enable Developer Mode";

        static string k_BuildReportAutoSaveKey = k_EditorPrefsPrefix + ".buildReportAutoSave";
        static string k_BuildReportAutoSaveLabel = "Auto Save Last Report";
        static bool k_BuildReportAutoSaveDefault = false;

        static string k_BuildReportPathKey = k_EditorPrefsPrefix + ".buildReportPath";
        static string k_BuildReportPathLabel = "Library Path";
        static string k_BuildReportPathDefault = "Assets/BuildReports";

        internal static string loadSavePath = string.Empty;

        public static string Path => k_PreferencesKey;

        /// <summary>
        /// If enabled, the BuildReport is automatically saved as asset after each build
        /// </summary>
        public static bool buildReportAutoSave
        {
            get => EditorPrefs.GetBool(k_BuildReportAutoSaveKey, k_BuildReportAutoSaveDefault);
            set => EditorPrefs.SetBool(k_BuildReportAutoSaveKey, value);
        }

        /// <summary>
        /// Customizable path to save the BuildReport
        /// </summary>
        public static string buildReportPath
        {
            get => EditorPrefs.GetString(k_BuildReportPathKey, k_BuildReportPathDefault);
            set => EditorPrefs.SetString(k_BuildReportPathKey, value);
        }

        public static bool developerMode
        {
            get => EditorPrefs.GetBool(k_DeveloperModeKey, false);
            set => EditorPrefs.SetBool(k_DeveloperModeKey, value);
        }

        public static bool logTimingsInfo
        {
            get => EditorPrefs.GetBool(k_LogTimingsInfoKey, false);
            set => EditorPrefs.SetBool(k_LogTimingsInfoKey, value);
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
                AssetDatabase.ImportAsset(ProjectAuditor.PackagePath + "/Editor/UserPreferences.cs");
            }
            logTimingsInfo = EditorGUILayout.Toggle(k_LogTimingsInfoLabel, logTimingsInfo);

            GUILayout.Space(10f);

            EditorGUILayout.LabelField("Build Report", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;

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

            EditorGUI.indentLevel--;
            EditorGUI.indentLevel--;
        }
    }
}
