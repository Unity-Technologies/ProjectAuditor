using System.Collections.Generic;

namespace Unity.ProjectAuditor.Editor.Core
{
    // stephenm TODO: Document
    public class AssetAnalysisContext : AnalysisContext
    {
        public string AssetPath;
    }

    // stephenm TODO: Document
    internal abstract class AssetsModuleAnalyzer : ModuleAnalyzer
    {
        public abstract IEnumerable<ReportItem> Analyze(AssetAnalysisContext context);
    }
}
