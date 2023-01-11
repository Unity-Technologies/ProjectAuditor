using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Unity.ProjectAuditor.Editor
{
    /// <summary>
    /// A built-in provider to manage ProjectAuditorSettings default and user-generated assets for customization.
    /// </summary>
    public class ProjectAuditorSettingsProvider : IProjectAuditorSettingsProvider
    {
        private HashSet<ProjectAuditorSettings> m_SettingsAssets = new HashSet<ProjectAuditorSettings>();
        private ProjectAuditorSettings m_CurrentSettings;
        private ProjectAuditorSettings m_DefaultSettings;

        public void Initialize()
        {
            m_DefaultSettings = ScriptableObject.CreateInstance<ProjectAuditorSettings>();
            m_DefaultSettings.name = "Default";

            RefreshAssets();
        }

        public void RefreshAssets()
        {
            m_CurrentSettings = m_DefaultSettings;

            var allSettingsAssets = AssetDatabase.FindAssets("t:ProjectAuditorSettings, a:assets");

            m_SettingsAssets.Clear();
            foreach (var assetGuid in allSettingsAssets)
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(assetGuid);
                var settingsAsset = AssetDatabase.LoadAssetAtPath<ProjectAuditorSettings>(assetPath);
                AddSettingsFromAsset(settingsAsset);
            }

            // restore settings
            var guidAsString = UserPreferences.settingsAsset;
            var guid = new GUID(guidAsString);

            if (guidAsString.Length > 0 && !guid.Empty())
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var settings = AssetDatabase.LoadAssetAtPath<ProjectAuditorSettings>(path);

                if (settings != null)
                    m_CurrentSettings = settings;
                else
                    SelectCurrentSettings(m_DefaultSettings);
            }
        }

        /// <summary>
        /// Returns <see cref="ProjectAuditorSettings"/> that were last selected, initially the default settings.
        /// </summary>
        public ProjectAuditorSettings GetCurrentSettings()
        {
            if (m_CurrentSettings == null)
            {
                m_CurrentSettings = m_DefaultSettings;
                RefreshAssets();
            }

            return m_CurrentSettings;
        }

        public IEnumerable<ProjectAuditorSettings> GetSettings()
        {
            yield return m_DefaultSettings;

            bool hasMissingAsset = false;
            foreach (var settingsAsset in m_SettingsAssets)
            {
                if (settingsAsset == null)
                {
                    hasMissingAsset = true;
                    continue;
                }

                yield return settingsAsset;
            }

            if (hasMissingAsset)
                RefreshAssets();
        }

        /// <summary>
        /// Adds a settings asset that was stored in the past or just created by the user.
        /// </summary>
        /// <param name="settingsAsset">A ScriptableObject asset of type <see cref="ProjectAuditorSettings"/> to be used as the settings to tweak analyzer values/limits.</param>
        public void AddSettingsFromAsset(ProjectAuditorSettings settingsAsset)
        {
            if (!m_SettingsAssets.Contains(settingsAsset))
            {
                m_SettingsAssets.Add(settingsAsset);
            }
        }

        /// <summary>
        /// Selects settings as the current settings. This may be the default settings or an asset with settings.
        /// </summary>
        /// <param name="settingsAsset">A ScriptableObject asset of type <see cref="ProjectAuditorSettings"/> to be used as the settings to tweak analyzer values/limits.</param>
        public void SelectCurrentSettings(ProjectAuditorSettings settings)
        {
            m_CurrentSettings = settings;

            var path = AssetDatabase.GetAssetPath(settings);
            var guid = AssetDatabase.GUIDFromAssetPath(path);

            var guidAsString = path == "" || guid.Empty() ? "" : guid.ToString();
            UserPreferences.settingsAsset = guidAsString;
        }
    }
}
