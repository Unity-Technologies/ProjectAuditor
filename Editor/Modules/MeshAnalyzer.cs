using System.Collections.Generic;
using System.IO;
using Unity.ProjectAuditor.Editor.Core;
using Unity.ProjectAuditor.Editor.Diagnostic;
using Unity.ProjectAuditor.Editor.Interfaces;
using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering;

namespace Unity.ProjectAuditor.Editor.Modules
{
    class MeshAnalyzer : IMeshModuleAnalyzer
    {
        internal const string PAA1000 = nameof(PAA1000);
        internal const string PAA1001 = nameof(PAA1001);

        internal static readonly Descriptor k_MeshReadWriteEnabledDescriptor = new Descriptor(
            PAA1000,
            "Mesh: Read/Write enabled",
            Area.Memory,
            "The <b>Read/Write Enabled</b> flag in the Model Import Settings is enabled. This causes the mesh data to be duplicated in memory.",
            "If not required, disable the <b>Read/Write Enabled</b> option in the Model Import Settings."
        )
        {
            MessageFormat = "Mesh '{0}' Read/Write is enabled",
            DocumentationUrl = "https://docs.unity3d.com/Manual/FBXImporter-Model.html"
        };

        internal static readonly Descriptor k_Mesh32BitIndexFormatUsedDescriptor = new Descriptor(
            PAA1001,
            "Mesh: Index Format is 32 bits",
            Area.Memory,
            "The <b>Index Format</b> in the Model Import Settings is set to <b>32 bit</b>. This increases the mesh size and may not work on certain mobile devices.",
            "Consider using changing the <b>Index Format</b> option in the Model Import Settings. This should be set to either <b>16 bits</b> or <b>Auto</b>."
        )
        {
            MessageFormat = "Mesh '{0}' Index Format is 32 bits",
            DocumentationUrl = "https://docs.unity3d.com/Manual/FBXImporter-Model.html"
        };

        public void Initialize(Module module)
        {
            module.RegisterDescriptor(k_MeshReadWriteEnabledDescriptor);
            module.RegisterDescriptor(k_Mesh32BitIndexFormatUsedDescriptor);
        }

        public IEnumerable<ProjectIssue> Analyze(MeshAnalysisContext context)
        {
            var assetPath = context.Importer.assetPath;
            var modelImporter = context.Importer as ModelImporter;
            var subAssets = AssetDatabase.LoadAllAssetsAtPath(assetPath);

            foreach (var subAsset in subAssets)
            {
                var mesh = subAsset as Mesh;
                if (mesh == null)
                    continue;

                var meshName = mesh.name;
                if (string.IsNullOrEmpty(meshName))
                    meshName = Path.GetFileNameWithoutExtension(assetPath);

                // TODO: the size returned by the profiler is not the exact size on the target platform. Needs to be fixed.
                var size = Profiler.GetRuntimeMemorySizeLong(mesh);

                yield return context.CreateWithoutDiagnostic(IssueCategory.Mesh, meshName)
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
                    yield return context.Create(IssueCategory.AssetDiagnostic, k_MeshReadWriteEnabledDescriptor.Id, meshName)
                        .WithLocation(assetPath);
                }

                if (mesh.indexFormat == IndexFormat.UInt32 &&
                    mesh.vertexCount <= 65535)
                {
                    yield return context.Create(IssueCategory.AssetDiagnostic,
                        k_Mesh32BitIndexFormatUsedDescriptor.Id, meshName)
                        .WithLocation(assetPath);
                }
            }
        }
    }
}
