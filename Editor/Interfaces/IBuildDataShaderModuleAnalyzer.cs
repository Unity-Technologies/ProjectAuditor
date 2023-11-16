using System.Collections.Generic;
using Unity.ProjectAuditor.Editor.Core;
using Unity.ProjectAuditor.Editor.Analyzer.SerializedObjects;

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
