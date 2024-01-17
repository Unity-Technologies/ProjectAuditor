using Unity.ProjectAuditor.Editor.Core;

namespace Unity.ProjectAuditor.Editor.Interfaces
{
    // stephenm TODO: Make this public, for extensibility. And, I guess, move it to API. Phase 2.
    internal interface IModuleAnalyzer
    {
        void Initialize(Module module);

        void CacheParameters(DiagnosticParams diagnosticParams);

        void RegisterParameters(DiagnosticParams diagnosticParams);
    }
}
