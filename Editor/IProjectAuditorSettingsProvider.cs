using UnityEditor;

namespace Unity.ProjectAuditor.Editor
{
    public interface IProjectAuditorSettingsProvider
    {
        public ProjectAuditorSettings GetOrCreateSettings(BuildTarget platform);
    }
}
