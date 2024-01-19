using System.Collections.Generic;
using Unity.ProjectAuditor.Editor.Core;
using UnityEditor;
using UnityEngine;

namespace Unity.ProjectAuditor.Editor.Interfaces
{
    internal class MeshAnalysisContext : AnalysisContext
    {
        public string Name;
        public Mesh Mesh;
        public AssetImporter Importer;
        public long Size;
    }

    internal abstract class MeshModuleAnalyzer : ModuleAnalyzer
    {
        public abstract IEnumerable<ReportItem> Analyze(MeshAnalysisContext context);
    }
}
