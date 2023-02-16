using System.Collections.Generic;
using Unity.ProjectAuditor.Editor.Core;
using Unity.ProjectAuditor.Editor.Profiling;

namespace Unity.ProjectAuditor.Editor.Modules
{
    public class ProfilingModule : ProjectAuditorModule
    {
        static ProfileAnalyzer m_ProfileAnalyzer = new ProfileAnalyzer();
        static public ProfileAnalyzer ProfileAnalyzer => m_ProfileAnalyzer;

        public override string name => "Profiling";

        public override IReadOnlyCollection<IssueLayout> supportedLayouts => new IssueLayout[] {};

        public override void Audit(ProjectAuditorParams projectAuditorParams, IProgress progress = null)
        {
            projectAuditorParams.onModuleCompleted();
        }
    }
}
