using System.Collections.Generic;
using Unity.ProjectAuditor.Editor.BuildData.SerializedObjects;
using Unity.ProjectAuditor.Editor.Core;

namespace Unity.ProjectAuditor.Editor.Interfaces
{
    class BuildDataAnimationClipAnalyzerContext : AnalysisContext
    {
        public AnimationClip AnimationClip;
    }

    internal interface IBuildDataAnimationClipModuleAnalyzer : IModuleAnalyzer
    {
        IEnumerable<ProjectIssue> Analyze(BuildDataAnimationClipAnalyzerContext context);
    }
}
