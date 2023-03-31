using System.Collections.Generic;

namespace Unity.ProjectAuditor.Editor
{
    internal interface IProjectAuditorSettingsProvider
    {
        void Initialize();

        IEnumerable<ProjectAuditorSettings> GetSettings();

        ProjectAuditorSettings GetCurrentSettings();

        void SelectCurrentSettings(ProjectAuditorSettings settings);
    }
}
