using UnityEditor;

namespace Unity.ProjectAuditor.Editor
{
    public interface IProjectAuditorSettingsProvider
    {
        public void Initialize();
        public ProjectAuditorSettings GetOrCreateSettings(BuildTarget platform);
    }
}
