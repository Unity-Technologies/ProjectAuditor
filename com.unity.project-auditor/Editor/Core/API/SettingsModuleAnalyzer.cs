using System.Collections.Generic;

namespace Unity.ProjectAuditor.Editor.Core
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
