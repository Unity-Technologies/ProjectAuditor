using Unity.ProjectAuditor.Editor.Core;

namespace Unity.ProjectAuditor.Editor.Core
{
    public interface IModuleAnalyzer
    {
        void Initialize(ProjectAuditorModule module);
    }
}
