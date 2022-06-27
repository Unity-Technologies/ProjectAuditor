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

        internal static string loadSavePath = string.Empty;

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
            logTimingsInfo = EditorGUILayout.Toggle(k_LogTimingsInfoLabel, logTimingsInfo);
            EditorGUI.indentLevel--;
        }
    }
}
