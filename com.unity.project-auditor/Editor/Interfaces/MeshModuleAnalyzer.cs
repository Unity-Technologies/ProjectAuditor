using System.Collections.Generic;
using Unity.ProjectAuditor.Editor.Core;
using UnityEditor;

namespace Unity.ProjectAuditor.Editor.Interfaces
{
    internal class MeshAnalysisContext : AnalysisContext
    {
        public AssetImporter Importer;
    }

    internal abstract class MeshModuleAnalyzer : ModuleAnalyzer
    {
        public abstract IEnumerable<ReportItem> Analyze(MeshAnalysisContext context);
    }
}
