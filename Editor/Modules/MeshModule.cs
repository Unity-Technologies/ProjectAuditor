using System;
using System.Collections.Generic;
using System.Linq;
using Unity.ProjectAuditor.Editor.Core;
using Unity.ProjectAuditor.Editor.Interfaces;
using UnityEditor;

namespace Unity.ProjectAuditor.Editor.Modules
{
    enum MeshProperty
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
            var analyzers = GetPlatformAnalyzers(projectAuditorParams.Platform);

            var rules = projectAuditorParams.Rules;
            var meshVertexCountLimit = rules.GetParameter("MeshVertexCountLimit", 5000);
            var meshTriangleCountLimit = rules.GetParameter("MeshTriangleCountLimit", 5000);

            var context = new MeshAnalysisContext()
            {
                // Importer is set in the loop
                Params = projectAuditorParams,
                MeshVertexCountLimit = meshVertexCountLimit,
                MeshTriangleCountLimit = meshTriangleCountLimit
            };

            var assetPaths = GetAssetPathsByFilter("t:mesh, a:assets", context);

            progress?.Start("Finding Meshes", "Search in Progress...", assetPaths.Length);

            foreach (var assetPath in assetPaths)
            {
                context.Importer = AssetImporter.GetAtPath(assetPath);

                foreach (var analyzer in analyzers)
                {
                    projectAuditorParams.OnIncomingIssues(analyzer.Analyze(context));
                }

                progress?.Advance();
            }

            progress?.Clear();

            projectAuditorParams.OnModuleCompleted?.Invoke();
        }
    }
}
