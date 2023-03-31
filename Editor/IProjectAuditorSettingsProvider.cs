using System.Collections.Generic;

namespace Unity.ProjectAuditor.Editor
{
    /// <summary>
    /// Provides methods for a way to gain access to a ProjectAuditorSettings object
    /// </summary>
    internal interface IProjectAuditorSettingsProvider
    {
        /// <summary>
        /// Initializes default ProjectAuditorSettings
        /// </summary>
        void Initialize();

        /// <summary>
        /// Gets all ProjectAuditorSettings objects
        /// </summary>
        /// <returns>An IEnumerable<ProjectAuditorSettings> of all the settings objects found in the project</returns>
        IEnumerable<ProjectAuditorSettings> GetSettings();

        /// <summary>
        /// Gets <see cref="ProjectAuditorSettings"/> that were last selected, initially the default settings.
        /// </summary>
        /// <returns>A ProjectAuditorSettings</returns>
        ProjectAuditorSettings GetCurrentSettings();

        /// <summary>
        /// Sets the current ProjectAuditorSettings object
        /// </summary>
        /// <param name="settings">The ProjectAuditorSettings object to be considered the current one.</param>
        void SelectCurrentSettings(ProjectAuditorSettings settings);
    }
}
