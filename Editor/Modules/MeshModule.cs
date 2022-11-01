using System;
using System.Collections.Generic;
using System.Linq;
using Unity.ProjectAuditor.Editor.Core;
using Unity.ProjectAuditor.Editor.Diagnostic;
using Unity.ProjectAuditor.Editor.Utils;
using UnityEngine.Profiling;
using UnityEditor;
using UnityEngine;

namespace Unity.ProjectAuditor.Editor.Modules
{
    public enum MeshProperty
    {
        VertexCount,
        TriangleCount,
        MeshCompression,
        SizeOnDisk,
        Platform,
        Num
    }

    class MeshModule : ProjectAuditorModule
    {
        static readonly IssueLayout k_MeshIssueLayout = new IssueLayout
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

        List<IMeshModuleAnalyzer> m_Analyzers;
        HashSet<Descriptor> m_DiagnosticDescriptors;

        public override IReadOnlyCollection<IssueLayout> supportedLayouts => new IssueLayout[]
        {
            k_MeshIssueLayout
        };

        public override IReadOnlyCollection<Descriptor> supportedDescriptors => m_DiagnosticDescriptors;

        public override void Initialize(ProjectAuditorConfig config)
        {
            m_Analyzers = new List<IMeshModuleAnalyzer>();
            m_DiagnosticDescriptors = new HashSet<Descriptor>();

            foreach (var type in TypeCache.GetTypesDerivedFrom(typeof(IMeshModuleAnalyzer)))
                AddAnalyzer(Activator.CreateInstance(type) as IMeshModuleAnalyzer);
        }

        public override void Audit(ProjectAuditorParams projectAuditorParams, IProgress progress = null)
        {
            var allMeshes = AssetDatabase.FindAssets("t:mesh, a:assets");
            var issues = new List<ProjectIssue>();
            var currentPlatform = projectAuditorParams.platform;

            progress?.Start("Finding Meshes", "Search in Progress...", allMeshes.Length);

            foreach (var guid in allMeshes)
            {
                var pathToMesh = AssetDatabase.GUIDToAssetPath(guid);
                var modelImporter = AssetImporter.GetAtPath(pathToMesh) as ModelImporter;
                if (modelImporter == null)
                {
                    continue;
                }

                var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(pathToMesh);
                var size = Profiler.GetRuntimeMemorySizeLong(mesh);

                var issue = ProjectIssue.Create(k_MeshIssueLayout.category, mesh.name)
                    .WithCustomProperties(
                        new object[((int)MeshProperty.Num)]
                        {
                            mesh.vertexCount,
                            mesh.triangles.Length / 3,
                            modelImporter.meshCompression,
                            size,
                            currentPlatform
                        })
                    .WithLocation(new Location(pathToMesh));

                issues.Add(issue);

                foreach (var analyzer in m_Analyzers)
                {
                    var platformDiagnostics = analyzer.Analyze(currentPlatform, modelImporter).ToArray();

                    issues.AddRange(platformDiagnostics);
                }

                progress?.Advance();
            }

            if (issues.Count > 0)
                projectAuditorParams.onIncomingIssues(issues);
            progress?.Clear();

            projectAuditorParams.onModuleCompleted?.Invoke();
        }

        void AddAnalyzer(IMeshModuleAnalyzer moduleAnalyzer)
        {
            moduleAnalyzer.Initialize(this);
            m_Analyzers.Add(moduleAnalyzer);
        }
    }
}
