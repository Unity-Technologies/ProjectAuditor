using System.Collections.Generic;
using Unity.ProjectAuditor.Editor.BuildData.SerializedObjects;
using Unity.ProjectAuditor.Editor.Core;

namespace Unity.ProjectAuditor.Editor.Interfaces
{
    class BuildDataAudioClipAnalyzerContext : AnalysisContext
    {
        public AudioClip AudioClip;
    }

    internal interface IBuildDataAudioClipModuleAnalyzer : IModuleAnalyzer
    {
        IEnumerable<ProjectIssue> Analyze(BuildDataAudioClipAnalyzerContext context);
    }
}
