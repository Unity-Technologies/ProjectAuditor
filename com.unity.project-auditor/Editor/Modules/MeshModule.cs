using System.Collections.Generic;
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

    class MeshModule : ModuleWithAnalyzers<IMeshModuleAnalyzer>
    {
        static readonly IssueLayout k_MeshLayout = new IssueLayout
        {
            Category = IssueCategory.Mesh,
            Properties = new[]
            {
                new PropertyDefinition { Type = PropertyType.Description, Format = PropertyFormat.String, Name = "Name", LongName = "Mesh Name", MaxAutoWidth = 500 },
                new PropertyDefinition { Type = PropertyTypeUtil.FromCustom(MeshProperty.VertexCount), Format = PropertyFormat.String, Name = "Vertex Count" },
                new PropertyDefinition { Type = PropertyTypeUtil.FromCustom(MeshProperty.TriangleCount), Format = PropertyFormat.String, Name = "Triangle Count" },
                new PropertyDefinition { Type = PropertyTypeUtil.FromCustom(MeshProperty.MeshCompression), Format = PropertyFormat.String, Name = "Compression", LongName = "Mesh Compression" },
                new PropertyDefinition { Type = PropertyTypeUtil.FromCustom(MeshProperty.SizeOnDisk), Format = PropertyFormat.Bytes, Name = "Size", LongName = "Mesh Size" },
                new PropertyDefinition { Type = PropertyType.Path, Name = "Path", MaxAutoWidth = 500 }
            }
        };

        public override string Name => "Meshes";

        public override IReadOnlyCollection<IssueLayout> SupportedLayouts => new IssueLayout[]
        {
            k_MeshLayout,
            AssetsModule.k_IssueLayout
        };

        const string k_MeshVertexCountLimit   = "MeshVertexCountLimit";
        const string k_MeshTriangleCountLimit = "MeshTriangleCountLimit";

        public override void RegisterParameters(DiagnosticParams diagnosticParams)
        {
            diagnosticParams.RegisterParameter(k_MeshVertexCountLimit, 5000);
            diagnosticParams.RegisterParameter(k_MeshTriangleCountLimit, 5000);
        }

        public override AnalysisResult Audit(AnalysisParams analysisParams, IProgress progress = null)
        {
            var analyzers = GetPlatformAnalyzers(analysisParams.Platform);

            var diagnosticParams = analysisParams.DiagnosticParams;
            var meshVertexCountLimit = diagnosticParams.GetParameter(k_MeshVertexCountLimit);
            var meshTriangleCountLimit = diagnosticParams.GetParameter(k_MeshTriangleCountLimit);

            var context = new MeshAnalysisContext()
            {
                // Importer is set in the loop
                Params = analysisParams,
                MeshVertexCountLimit = meshVertexCountLimit,
                MeshTriangleCountLimit = meshTriangleCountLimit
            };

            var assetPaths = GetAssetPathsByFilter("t:mesh, a:assets", context);

            progress?.Start("Finding Meshes", "Search in Progress...", assetPaths.Length);

            foreach (var assetPath in assetPaths)
            {
                if (progress?.IsCancelled ?? false)
                    return AnalysisResult.Cancelled;

                context.Importer = AssetImporter.GetAtPath(assetPath);

                foreach (var analyzer in analyzers)
                {
                    analysisParams.OnIncomingIssues(analyzer.Analyze(context));
                }

                progress?.Advance();
            }

            progress?.Clear();

            return AnalysisResult.Success;
        }
    }
}
