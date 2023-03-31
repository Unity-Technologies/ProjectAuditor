using Unity.ProjectAuditor.Editor.Core;

namespace Unity.ProjectAuditor.Editor.Core
{
    internal interface IModuleAnalyzer
    {
        void Initialize(ProjectAuditorModule module);
    }
}
