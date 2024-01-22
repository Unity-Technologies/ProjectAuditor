using System.Collections.Generic;
using Unity.ProjectAuditor.Editor.Core;

namespace Unity.ProjectAuditor.Editor.Interfaces
{
    // stephenm TODO: Document
    public class SpriteAtlasAnalysisContext : AnalysisContext
    {
        public string AssetPath;
    }

    // stephenm TODO: Document
    internal abstract class SpriteAtlasModuleAnalyzer : ModuleAnalyzer
    {
        public abstract IEnumerable<ReportItem> Analyze(SpriteAtlasAnalysisContext context);
    }
}
