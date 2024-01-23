using System.Collections.Generic;
using Unity.ProjectAuditor.Editor.Core;
using UnityEditor;
using UnityEngine;

namespace Unity.ProjectAuditor.Editor.Core
{
    // stephenm TODO: Document
    public class MeshAnalysisContext : AnalysisContext
    {
        public string Name;
        public Mesh Mesh;
        public AssetImporter Importer;
        public long Size;
    }

    // stephenm TODO: Document
    internal abstract class MeshModuleAnalyzer : ModuleAnalyzer
    {
        public abstract IEnumerable<ReportItem> Analyze(MeshAnalysisContext context);
    }
}
