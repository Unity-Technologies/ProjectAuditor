using System.Collections.Generic;
using Unity.ProjectAuditor.Editor.Core;

namespace Unity.ProjectAuditor.Editor.Interfaces
{
    internal class SpriteAtlasAnalysisContext : AnalysisContext
    {
        public string AssetPath;
    }

    internal interface ISpriteAtlasModuleAnalyzer : IModuleAnalyzer
    {
        IEnumerable<ReportItem> Analyze(SpriteAtlasAnalysisContext context);
    }
}
