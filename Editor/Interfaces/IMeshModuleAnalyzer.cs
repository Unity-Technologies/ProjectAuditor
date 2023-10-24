using System.Collections.Generic;
using Unity.ProjectAuditor.Editor.Core;
using UnityEditor;

namespace Unity.ProjectAuditor.Editor.Interfaces
{
    internal class MeshAnalysisContext : AnalysisContext
    {
        public AssetImporter Importer;
    }

    internal interface IMeshModuleAnalyzer : IModuleAnalyzer
    {
        IEnumerable<ProjectIssue> Analyze(MeshAnalysisContext context);
    }
}
