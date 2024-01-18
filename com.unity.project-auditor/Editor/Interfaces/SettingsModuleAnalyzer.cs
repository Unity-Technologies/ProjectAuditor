using System.Collections.Generic;
using Unity.ProjectAuditor.Editor.Core;

namespace Unity.ProjectAuditor.Editor.Interfaces
{
    internal class SettingsAnalysisContext : AnalysisContext
    {
    }

    internal abstract class SettingsModuleAnalyzer : ModuleAnalyzer
    {
        public abstract IEnumerable<ReportItem> Analyze(SettingsAnalysisContext context);
    }
}
