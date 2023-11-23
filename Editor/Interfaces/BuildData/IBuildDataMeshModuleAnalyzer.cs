using System.Collections.Generic;
using Unity.ProjectAuditor.Editor.BuildData.SerializedObjects;
using Unity.ProjectAuditor.Editor.Core;

namespace Unity.ProjectAuditor.Editor.Interfaces
{
    class BuildDataMeshAnalyzerContext : AnalysisContext
    {
        public Mesh Mesh;
    }

    internal interface IBuildDataMeshModuleAnalyzer : IModuleAnalyzer
    {
        IEnumerable<ProjectIssue> Analyze(BuildDataMeshAnalyzerContext context);
    }
}
