using System.Collections.Generic;
using Unity.ProjectAuditor.Editor.Core;

namespace Unity.ProjectAuditor.Editor.Interfaces
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
