using System.Collections.Generic;
using Unity.ProjectAuditor.Editor.Core;

namespace Unity.ProjectAuditor.Editor.Interfaces
{
    internal class SpriteAtlasAnalysisContext : AnalysisContext
    {
        public string AssetPath;
        public int SpriteAtlasEmptySpaceLimit;
    }

    internal interface ISpriteAtlasModuleAnalyzer : IModuleAnalyzer
    {
        IEnumerable<ProjectIssue> Analyze(SpriteAtlasAnalysisContext context);
    }
}
