using System.Collections.Generic;
using Unity.ProjectAuditor.Editor.Core;

namespace Unity.ProjectAuditor.Editor.Interfaces
{
    // stephenm TODO: Document
    public class SettingsAnalysisContext : AnalysisContext
    {
    }

    // stephenm TODO: Document
    internal abstract class SettingsModuleAnalyzer : ModuleAnalyzer
    {
        public abstract IEnumerable<ReportItem> Analyze(SettingsAnalysisContext context);
    }
}
