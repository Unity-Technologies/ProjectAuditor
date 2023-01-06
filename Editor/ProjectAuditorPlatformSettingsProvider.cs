using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Unity.ProjectAuditor.Editor
{
    /// <summary>
    /// A built-in provider to return ProjectAuditorSettings for any platform.
    /// </summary>
    public class ProjectAuditorPlatformSettingsProvider : IProjectAuditorSettingsProvider
    {
        private Dictionary<BuildTarget, ProjectAuditorSettings> m_Settings = new Dictionary<BuildTarget, ProjectAuditorSettings>();

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
            if (m_Settings.ContainsKey(platform))
            {
                Debug.LogWarningFormat("ProjectAuditorPlatformSettingsProvider: Settings for platform '{0}' already exists been created.", platform.ToString());
                return m_Settings[platform];
            }

            var newSettings = new ProjectAuditorSettings();

            m_Settings.Add(platform, newSettings);

            return newSettings;
        }
    }
}
