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

        const string k_MeshVertexCountLimit   = "MeshVertexCountLimit";
        const string k_MeshTriangleCountLimit = "MeshTriangleCountLimit";

        public override void RegisterParameters(DiagnosticParams diagnosticParams)
        {
            diagnosticParams.RegisterParameter(k_MeshVertexCountLimit, 5000);
            diagnosticParams.RegisterParameter(k_MeshTriangleCountLimit, 5000);
        }

        public override void Audit(AnalysisParams analysisParams, IProgress progress = null)
        {
            var analyzers = GetPlatformAnalyzers(analysisParams.Platform);

            var assetPaths = GetAssetPathsByFilter("t:mesh, a:assets");

            progress?.Start("Finding Meshes", "Search in Progress...", assetPaths.Length);

            var diagnosticParams = analysisParams.DiagnosticParams;
            var meshVertexCountLimit = diagnosticParams.GetParameter(k_MeshVertexCountLimit);
            var meshTriangleCountLimit = diagnosticParams.GetParameter(k_MeshTriangleCountLimit);

            foreach (var assetPath in assetPaths)
            {
                var context = new MeshAnalysisContext()
                {
                    Importer = AssetImporter.GetAtPath(assetPath),
                    Params = analysisParams,
                    MeshVertexCountLimit = meshVertexCountLimit,
                    MeshTriangleCountLimit = meshTriangleCountLimit
                };

                foreach (var analyzer in analyzers)
                {
                    analysisParams.OnIncomingIssues(analyzer.Analyze(context));
                }

                progress?.Advance();
            }

            progress?.Clear();

            analysisParams.OnModuleCompleted?.Invoke();
        }
    }
}
