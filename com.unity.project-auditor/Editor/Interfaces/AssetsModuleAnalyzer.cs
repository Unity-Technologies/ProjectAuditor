using System.Collections.Generic;
using Unity.ProjectAuditor.Editor.Core;

namespace Unity.ProjectAuditor.Editor.Interfaces
{
    internal class AssetAnalysisContext : AnalysisContext
    {
        public string AssetPath;
    }

    internal abstract class AssetsModuleAnalyzer : ModuleAnalyzer
    {
        public abstract IEnumerable<ReportItem> Analyze(AssetAnalysisContext context);
    }
}
