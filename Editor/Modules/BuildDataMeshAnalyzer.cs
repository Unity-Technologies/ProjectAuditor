using System.Collections.Generic;
using Unity.ProjectAuditor.Editor.Core;
using Unity.ProjectAuditor.Editor.Interfaces;

namespace Unity.ProjectAuditor.Editor.Modules
{
    class BuildDataMeshAnalyzer : IBuildDataMeshModuleAnalyzer
    {
        public void Initialize(Module module)
        {
        }

        public IEnumerable<ProjectIssue> Analyze(BuildDataMeshAnalyzerContext context)
        {
            string channelsAsString = "";
            foreach (var channel in context.Mesh.Channels)
            {
                if (channelsAsString.Length > 0)
                    channelsAsString += ", ";

                channelsAsString += channel.Usage.ToString() + " " + channel.Type.ToString() + "[" + channel.Dimension + "]";
            }

            yield return context.CreateWithoutDiagnostic(IssueCategory.BuildDataMesh, context.Mesh.Name)
                .WithCustomProperties(
                    new object[((int)BuildDataMeshProperty.Num)]
                    {
                        context.Mesh.BuildFile.Path,
                        context.Mesh.Size,
                        context.Mesh.SubMeshes,
                        context.Mesh.BlendShapes,
                        context.Mesh.Bones,
                        context.Mesh.Indices,
                        context.Mesh.Vertices,
                        context.Mesh.Compression,
                        context.Mesh.RwEnabled,
                        context.Mesh.VertexSize,
                        channelsAsString,
                    });
        }
    }
}
