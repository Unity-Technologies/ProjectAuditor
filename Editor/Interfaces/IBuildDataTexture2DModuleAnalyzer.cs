using System.Collections.Generic;
using Unity.ProjectAuditor.Editor.BuildData.SerializedObjects;
using Unity.ProjectAuditor.Editor.Core;

namespace Unity.ProjectAuditor.Editor.Interfaces
{
    class BuildDataTexture2DAnalyzerContext : AnalysisContext
    {
        public Texture2D Texture;
    }

    internal interface IBuildDataTexture2DModuleAnalyzer : IModuleAnalyzer
    {
        IEnumerable<ProjectIssue> Analyze(BuildDataTexture2DAnalyzerContext context);
    }
}
