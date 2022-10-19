using System.Collections.Generic;
using UnityEditor;

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

        internal static string loadSavePath = string.Empty;

        public static string Path => k_PreferencesKey;

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
            EditorGUI.indentLevel++;
            var value = EditorGUILayout.Toggle(k_DeveloperModeLabel, developerMode);
            if (value != developerMode)
            {
                developerMode = value;

                // need to trigger domain reload so that Views are re-registered
                AssetDatabase.ImportAsset(ProjectAuditor.PackagePath + "/Editor/UserPreferences.cs");
            }
            logTimingsInfo = EditorGUILayout.Toggle(k_LogTimingsInfoLabel, logTimingsInfo);
            EditorGUI.indentLevel--;
        }
    }
}
