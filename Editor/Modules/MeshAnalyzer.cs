using System.Collections.Generic;
using System.IO;
using Unity.ProjectAuditor.Editor.Core;
using Unity.ProjectAuditor.Editor.Diagnostic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering;

namespace Unity.ProjectAuditor.Editor.Modules
{
    public class MeshAnalyzer : IMeshModuleAnalyzer
    {
        internal static readonly Descriptor k_MeshReadWriteEnabledDescriptor = new Descriptor(
            "PAM0000",
            "Mesh: Read/Write enabled",
            Area.Memory,
            "Mesh's Read/Write flag is enabled. This causes the mesh data to be duplicated in memory." +
            "\n\nEnabling Read/Write access may be needed if this mesh is read or written to at run-time, e.g. if used as a MeshCollider or a BlendShape. Polybrush meshes may set this by default, so again save to disable if not modified at run-time." +
            "\n\nRefer to the 'Mesh.isReadable' documentation for more details on cases when this should stay enabled.",
            "Select the asset and disable <b>Model</b> import settings <b>Read/Write Enabled</b> option, then click the <b>Apply</b> button."
        )
        {
            messageFormat = "Mesh '{0}' Read/Write is enabled",
            documentationUrl = "https://docs.unity3d.com/ScriptReference/Mesh-isReadable.html"
        };

        internal static readonly Descriptor k_Mesh32BitIndexFormatUsedDescriptor = new Descriptor(
            "PAM0001",
            "Mesh: Index Format is 32 bits",
            Area.Memory,
            "Mesh's 32 bits Index Format is selected. This increases the mesh size and may not work on certain mobile devices.",
            "Select the asset and set the <b>Model</b> import settings <b>Index Format</b> option to the value <b>16 bits</b>."
        )
        {
            messageFormat = "Mesh '{0}' Index Format is 32 bits",
            documentationUrl = "https://docs.unity3d.com/ScriptReference/Mesh-indexFormat.html"
        };

        public void Initialize(ProjectAuditorModule module)
        {
            module.RegisterDescriptor(k_MeshReadWriteEnabledDescriptor);
            module.RegisterDescriptor(k_Mesh32BitIndexFormatUsedDescriptor);
        }

        public IEnumerable<ProjectIssue> Analyze(ProjectAuditorParams projectAuditorParams, AssetImporter assetImporter)
        {
            var assetPath = assetImporter.assetPath;
            var modelImporter = assetImporter as ModelImporter;
            var subAssets = AssetDatabase.LoadAllAssetsAtPath(assetPath);

            foreach (var subAsset in subAssets)
            {
                var mesh = subAsset as Mesh;
                if (mesh == null)
                    continue;

                // TODO: the size returned by the profiler is not the exact size on the target platform. Needs to be fixed.
                var size = Profiler.GetRuntimeMemorySizeLong(mesh);

                yield return ProjectIssue.Create(IssueCategory.Mesh, mesh.name)
                    .WithCustomProperties(
                        new object[((int)MeshProperty.Num)]
                        {
                            mesh.vertexCount,
                            mesh.triangles.Length / 3,
                            modelImporter != null
                            ? modelImporter.meshCompression
                            : ModelImporterMeshCompression.Off,
                            size
                        })
                    .WithLocation(new Location(assetPath));

                if (mesh.isReadable)
                {
                    yield return ProjectIssue.Create(IssueCategory.AssetDiagnostic, k_MeshReadWriteEnabledDescriptor, mesh.name)
                        .WithLocation(assetPath);
                }

                if (mesh.indexFormat == IndexFormat.UInt32 &&
                    mesh.vertexCount <= 65535)
                {
                    yield return ProjectIssue.Create(IssueCategory.AssetDiagnostic,
                        k_Mesh32BitIndexFormatUsedDescriptor, mesh.name)
                        .WithLocation(assetPath);
                }
            }
        }
    }
}
