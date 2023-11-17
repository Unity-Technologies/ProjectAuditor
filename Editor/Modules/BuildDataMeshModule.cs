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
            category = IssueCategory.BuildDataMesh,
            properties = new[]
            {
                new PropertyDefinition { type = PropertyType.Description, format = PropertyFormat.String, name = "Name", longName = "Mesh Name" },
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(BuildDataMeshProperty.AssetBundle), format = PropertyFormat.String, name = "File", longName = "File Name" },
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(BuildDataMeshProperty.Size), format = PropertyFormat.Bytes, name = "Size", longName = "Size" },
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(BuildDataMeshProperty.SubMeshes), format = PropertyFormat.Integer, name = "Sub Meshes", longName = "Number Of Sub Meshes" },
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(BuildDataMeshProperty.BlendShapes), format = PropertyFormat.Integer, name = "Blend Shapes", longName = "Number Of Blend Shapes" },
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(BuildDataMeshProperty.Bones), format = PropertyFormat.Integer, name = "Bones", longName = "Number Of Bones" },
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(BuildDataMeshProperty.Indices), format = PropertyFormat.Integer, name = "Indices", longName = "Number Of Indices" },
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(BuildDataMeshProperty.Vertices), format = PropertyFormat.Integer, name = "Vertices", longName = "Number Of Vertices" },
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(BuildDataMeshProperty.Compression), format = PropertyFormat.Integer, name = "Compression", longName = "Compression" },
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(BuildDataMeshProperty.RwEnabled), format = PropertyFormat.Bool, name = "Rw Enabled", longName = "Read/Write Is Enabled" },
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(BuildDataMeshProperty.VertexSize), format = PropertyFormat.Integer, name = "Vertex Size", longName = "Vertex Size In Bytes" },
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(BuildDataMeshProperty.Channels), format = PropertyFormat.String, name = "Channels", longName = "Used Vertex Channels" },
            }
        };

        public override string Name => "Meshes";

        public override bool IsEnabledByDefault => false;

        public override IReadOnlyCollection<IssueLayout> SupportedLayouts => new IssueLayout[]
        {
            k_MeshLayout
        };

        public override void Audit(AnalysisParams projectAuditorParams, IProgress progress = null)
        {
            var analyzers = GetPlatformAnalyzers(projectAuditorParams.Platform);

            if (projectAuditorParams.BuildAnalyzer != null)
            {
                var meshes = projectAuditorParams.BuildAnalyzer.GetSerializedObjects<Mesh>();

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

            projectAuditorParams.OnModuleCompleted?.Invoke();
        }
    }
}
