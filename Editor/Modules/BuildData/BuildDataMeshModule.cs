using System.Collections.Generic;
using System.Linq;
using Unity.ProjectAuditor.Editor.Core;
using Unity.ProjectAuditor.Editor.Interfaces;
using Unity.ProjectAuditor.Editor.BuildData.SerializedObjects;

namespace Unity.ProjectAuditor.Editor.Modules
{
    enum BuildDataMeshProperty
    {
        AssetBundle,
        Size,
        SubMeshes,
        BlendShapes,
        Bones,
        Indices,
        Vertices,
        Compression,
        RwEnabled,
        VertexSize,
        Channels,
        Num,
    }

    class BuildDataMeshModule : ModuleWithAnalyzers<IBuildDataMeshModuleAnalyzer>
    {
        static readonly IssueLayout k_MeshLayout = new IssueLayout
        {
            Category = IssueCategory.BuildDataMesh,
            Properties = new[]
            {
                new PropertyDefinition { Type = PropertyType.Description, Format = PropertyFormat.String, Name = "Name", LongName = "Mesh Name" },
                new PropertyDefinition { Type = PropertyTypeUtil.FromCustom(BuildDataMeshProperty.AssetBundle), Format = PropertyFormat.String, Name = "File", LongName = "File Name" },
                new PropertyDefinition { Type = PropertyTypeUtil.FromCustom(BuildDataMeshProperty.Size), Format = PropertyFormat.Bytes, Name = "Size", LongName = "Size" },
                new PropertyDefinition { Type = PropertyTypeUtil.FromCustom(BuildDataMeshProperty.SubMeshes), Format = PropertyFormat.Integer, Name = "Sub Meshes", LongName = "Number Of Sub Meshes" },
                new PropertyDefinition { Type = PropertyTypeUtil.FromCustom(BuildDataMeshProperty.BlendShapes), Format = PropertyFormat.Integer, Name = "Blend Shapes", LongName = "Number Of Blend Shapes" },
                new PropertyDefinition { Type = PropertyTypeUtil.FromCustom(BuildDataMeshProperty.Bones), Format = PropertyFormat.Integer, Name = "Bones", LongName = "Number Of Bones" },
                new PropertyDefinition { Type = PropertyTypeUtil.FromCustom(BuildDataMeshProperty.Indices), Format = PropertyFormat.Integer, Name = "Indices", LongName = "Number Of Indices" },
                new PropertyDefinition { Type = PropertyTypeUtil.FromCustom(BuildDataMeshProperty.Vertices), Format = PropertyFormat.Integer, Name = "Vertices", LongName = "Number Of Vertices" },
                new PropertyDefinition { Type = PropertyTypeUtil.FromCustom(BuildDataMeshProperty.Compression), Format = PropertyFormat.String, Name = "Compression", LongName = "Compression" },
                new PropertyDefinition { Type = PropertyTypeUtil.FromCustom(BuildDataMeshProperty.RwEnabled), Format = PropertyFormat.Bool, Name = "Rw Enabled", LongName = "Read/Write Is Enabled" },
                new PropertyDefinition { Type = PropertyTypeUtil.FromCustom(BuildDataMeshProperty.VertexSize), Format = PropertyFormat.Integer, Name = "Vertex Size", LongName = "Vertex Size In Bytes" },
                new PropertyDefinition { Type = PropertyTypeUtil.FromCustom(BuildDataMeshProperty.Channels), Format = PropertyFormat.String, Name = "Channels", LongName = "Used Vertex Channels" },
            }
        };

        public override string Name => "BuildDataMeshes";

        public override IReadOnlyCollection<IssueLayout> SupportedLayouts => new IssueLayout[]
        {
            k_MeshLayout
        };

        public override AnalysisResult Audit(AnalysisParams projectAuditorParams, IProgress progress = null)
        {
            var analyzers = GetPlatformAnalyzers(projectAuditorParams.Platform);

            if (projectAuditorParams.BuildObjects != null)
            {
                var meshes = projectAuditorParams.BuildObjects.GetObjects<Mesh>();

                progress?.Start("Parsing Meshes from Build Data", "Search in Progress...", meshes.Count());

                foreach (var mesh in meshes)
                {
                    foreach (var analyzer in analyzers)
                    {
                        var context = new BuildDataMeshAnalyzerContext
                        {
                            Mesh = mesh,
                            Params = projectAuditorParams
                        };

                        projectAuditorParams.OnIncomingIssues(analyzer.Analyze(context));
                    }

                    progress?.Advance();
                }

                progress?.Clear();
            }

            return AnalysisResult.Success;
        }
    }
}
