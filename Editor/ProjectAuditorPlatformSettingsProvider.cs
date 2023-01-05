using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Unity.ProjectAuditor.Editor
{
    /// <summary>
    /// A built-in provider to return ProjectAuditorSettings for any platform. Settings are stored as an asset and editable by the user.
    /// </summary>
    public class ProjectAuditorPlatformSettingsProvider : IProjectAuditorSettingsProvider
    {
        private const string DefaultSettingsAssetPath = "Assets/Editor/ProjectAuditorSettings";
        private Dictionary<BuildTarget, ProjectAuditorSettings> m_Settings = new Dictionary<BuildTarget, ProjectAuditorSettings>();

        public void Initialize()
        {
            AddPlatformSettings(BuildTarget.NoTarget);
        }

        /// <summary>
        /// Returns ProjectAuditorSettings for the provided platform, to be used by analyzers that have configurable values/limits.
        /// </summary>
        /// <param name="platform">The specific platform to get settings for. This provider guarantees a setting, so it may fall back to create a new setting for the target/platform.</param>
        public ProjectAuditorSettings GetOrCreateSettings(BuildTarget platform)
        {
            foreach (var settingsEntry in m_Settings)
            {
                if (settingsEntry.Key == platform)
                {
                    return settingsEntry.Value;
                }
            }

            var settings = AddPlatformSettings(platform);

            return settings;
        }

        /// <summary>
        /// Adds a settings asset or returns an existing one. This is called once per ProjectAuditor session and platform to prepare for any analyzers using configurable values/limits.
        /// </summary>
        /// <param name="platform">Adds settings for a specific platform to provide settings for; NoTarget value is used for default settings, as a fallback for any target/platform.</param>
        public ProjectAuditorSettings AddPlatformSettings(BuildTarget platform)
        {
            var assetPath = $"{DefaultSettingsAssetPath}-{platform.ToString()}.asset";

            if (m_Settings.ContainsKey(platform))
            {
                Debug.LogWarningFormat("ProjectAuditorPlatformSettingsProvider: {0} settings for platform '{1}' already exists been created.", assetPath, platform.ToString());
                return m_Settings[platform];
            }

            var newSettings = AssetDatabase.LoadAssetAtPath<ProjectAuditorSettings>(assetPath);
            if (newSettings == null)
            {
                var path = Path.GetDirectoryName(assetPath);
                if (!File.Exists(path))
                    Directory.CreateDirectory(path);
                newSettings = ScriptableObject.CreateInstance<ProjectAuditorSettings>();
                AssetDatabase.CreateAsset(newSettings, assetPath);

                Debug.LogFormat("ProjectAuditorSettingsProvider: {0} has been created.", assetPath);
            }

            m_Settings.Add(platform, newSettings);

            return newSettings;
        }
    }
}
