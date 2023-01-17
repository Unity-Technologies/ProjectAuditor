using System;
using System.Collections.Generic;
using System.Linq;
using Unity.ProjectAuditor.Editor.Core;
using Unity.ProjectAuditor.Editor.Diagnostic;
using UnityEditor;

namespace Unity.ProjectAuditor.Editor.Modules
{
    public enum MeshProperty
    {
        VertexCount,
        TriangleCount,
        MeshCompression,
        SizeOnDisk,
        Num
    }

    class MeshModule : ProjectAuditorModuleWithAnalyzers<IMeshModuleAnalyzer>
    {
        static readonly IssueLayout k_MeshLayout = new IssueLayout
        {
            category = IssueCategory.Mesh,
            properties = new[]
            {
                new PropertyDefinition { type = PropertyType.Description, format = PropertyFormat.String, name = "Name", longName = "Mesh Name" },
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(MeshProperty.VertexCount), format = PropertyFormat.String, name = "Vertex Count" },
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(MeshProperty.TriangleCount), format = PropertyFormat.String, name = "Triangle Count" },
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(MeshProperty.MeshCompression), format = PropertyFormat.String, name = "Compression", longName = "Mesh Compression" },
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(MeshProperty.SizeOnDisk), format = PropertyFormat.Bytes, name = "Size", longName = "Mesh Size" },
                new PropertyDefinition { type = PropertyType.Path, name = "Path"}
            }
        };

        public override string name => "Meshes";

        public override bool isEnabledByDefault => false;

        public override IReadOnlyCollection<IssueLayout> supportedLayouts => new IssueLayout[]
        {
            k_MeshLayout,
            AssetsModule.k_IssueLayout
        };

        public override void Audit(ProjectAuditorParams projectAuditorParams, IProgress progress = null)
        {
            var analyzers = GetPlatformAnalyzers(projectAuditorParams.platform);
            var allMeshes = AssetDatabase.FindAssets("t:mesh, a:assets");

            progress?.Start("Finding Meshes", "Search in Progress...", allMeshes.Length);

            foreach (var guid in allMeshes)
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(guid);
                var importer = AssetImporter.GetAtPath(assetPath);

                foreach (var analyzer in analyzers)
                {
                    projectAuditorParams.onIncomingIssues(analyzer.Analyze(projectAuditorParams, importer));
                }

                progress?.Advance();
            }

            progress?.Clear();

            projectAuditorParams.onModuleCompleted?.Invoke();
        }
    }
}
