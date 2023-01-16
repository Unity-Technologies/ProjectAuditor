using System.Collections.Generic;

namespace Unity.ProjectAuditor.Editor
{
    public interface IProjectAuditorSettingsProvider
    {
        public void Initialize();

        public IEnumerable<ProjectAuditorSettings> GetSettings();

        public ProjectAuditorSettings GetCurrentSettings();
        public void SelectCurrentSettings(ProjectAuditorSettings settings);
    }
}
