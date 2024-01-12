using System.Collections.Generic;
using Unity.ProjectAuditor.Editor.Core;

namespace Unity.ProjectAuditor.Editor.Interfaces
{
    internal class AssetAnalysisContext : AnalysisContext
    {
        public string AssetPath;
    }

    internal interface IAssetsModuleAnalyzer : IModuleAnalyzer
    {
        IEnumerable<ReportItem> Analyze(AssetAnalysisContext context);
    }
}
