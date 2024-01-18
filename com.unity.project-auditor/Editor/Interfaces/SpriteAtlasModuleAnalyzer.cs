using System.Collections.Generic;
using Unity.ProjectAuditor.Editor.Core;

namespace Unity.ProjectAuditor.Editor.Interfaces
{
    internal class SpriteAtlasAnalysisContext : AnalysisContext
    {
        public string AssetPath;
    }

    internal abstract class SpriteAtlasModuleAnalyzer : ModuleAnalyzer
    {
        public abstract IEnumerable<ReportItem> Analyze(SpriteAtlasAnalysisContext context);
    }
}
