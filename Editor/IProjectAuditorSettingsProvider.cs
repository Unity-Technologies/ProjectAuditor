using System.Collections.Generic;

namespace Unity.ProjectAuditor.Editor
{
    public interface IProjectAuditorSettingsProvider
    {
        void Initialize();

        IEnumerable<ProjectAuditorSettings> GetSettings();

        ProjectAuditorSettings GetCurrentSettings();

        void SelectCurrentSettings(ProjectAuditorSettings settings);
    }
}
