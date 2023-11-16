using System.Collections.Generic;
using Unity.ProjectAuditor.Editor.BuildData.SerializedObjects;
using Unity.ProjectAuditor.Editor.Core;

namespace Unity.ProjectAuditor.Editor.Interfaces
{
    class BuildDataShaderAnalyzerContext : AnalysisContext
    {
        public Shader Shader;
    }

    internal interface IBuildDataShaderModuleAnalyzer : IModuleAnalyzer
    {
        IEnumerable<ProjectIssue> Analyze(BuildDataShaderAnalyzerContext context);
    }
}
