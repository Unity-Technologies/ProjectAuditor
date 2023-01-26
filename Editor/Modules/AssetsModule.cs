using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.ProjectAuditor.Editor.Core;
using Unity.ProjectAuditor.Editor.Diagnostic;
using Unity.ProjectAuditor.Editor.Utils;
using UnityEditor;
using UnityEngine;

namespace Unity.ProjectAuditor.Editor.Modules
{
    internal class AssetsModule : ProjectAuditorModule
    {
        internal static readonly IssueLayout k_IssueLayout = new IssueLayout
        {
            category = IssueCategory.AssetDiagnostic,
            properties = new[]
            {
                new PropertyDefinition { type = PropertyType.Description, name = "Issue", longName = "Issue description"},
                new PropertyDefinition { type = PropertyType.Severity, format = PropertyFormat.String, name = "Severity"},
                new PropertyDefinition { type = PropertyType.Area, format = PropertyFormat.String, name = "Area", longName = "Impacted Area" },
                new PropertyDefinition { type = PropertyType.Path, name = "Path"},
                new PropertyDefinition { type = PropertyType.Descriptor, name = "Descriptor", defaultGroup = true, hidden = true},
            }
        };

        static readonly Descriptor k_ResourcesFolderDescriptor = new Descriptor
            (
            "PAA0000",
            "Resources folder asset & dependencies",
            Area.BuildSize,
            "The Resources folder is a common source of many problems in Unity projects. Improper use of the Resources folder can bloat the size of a project’s build, lead to uncontrollable excessive memory utilization, and significantly increase application startup times.",
            "Use AssetBundles when possible"
            )
        {
            messageFormat = "'{0}' {1}"
        };

        static readonly Descriptor k_StreamingAssetsFolderDescriptor = new Descriptor(
            "PAA0001",
            "StreamingAssets folder size",
            Area.BuildSize,
            $"There are many files in the 'Assets/StreamingAssets' folder. Keeping them in the StreamingAssets folder will increase the build size.",
            $"Try to move files outside this folder and use Asset Bundles or Addressables when possible."
        )
        {
            platforms = new[] {"Android", "iOS"},
            messageFormat = "StreamingAssets folder contains {0} of data",
        };

        public override bool isEnabledByDefault => false;

        public override string name => "Assets";

        public override IReadOnlyCollection<IssueLayout> supportedLayouts => new IssueLayout[] {k_IssueLayout};

        public override void Initialize(ProjectAuditorConfig config)
        {
            base.Initialize(config);

            RegisterDescriptor(k_ResourcesFolderDescriptor);
            RegisterDescriptor(k_StreamingAssetsFolderDescriptor);
        }

        public override void RegisterDescriptor(Descriptor descriptor)
        {
            if (!m_Descriptors.Add(descriptor))
                throw new Exception("Duplicate descriptor with id: " + descriptor.id);
        }

        public override void Audit(ProjectAuditorParams projectAuditorParams, IProgress progress = null)
        {
            var settings = projectAuditorParams.settings;

            var issues = new List<ProjectIssue>();
            AnalyzeResources(issues);

            if (k_StreamingAssetsFolderDescriptor.platforms.Contains(projectAuditorParams.platform.ToString()))
                AnalyzeStreamingAssets(settings, issues);

            if (issues.Any())
                projectAuditorParams.onIncomingIssues(issues);
            projectAuditorParams.onModuleCompleted?.Invoke();
        }

        static void AnalyzeResources(IList<ProjectIssue> issues)
        {
            var allAssetPaths = AssetDatabase.GetAllAssetPaths();
            var allResources =
                allAssetPaths.Where(path => path.IndexOf("/resources/", StringComparison.OrdinalIgnoreCase) >= 0);
            var allPlayerResources =
                allResources.Where(path => path.IndexOf("/editor/", StringComparison.OrdinalIgnoreCase) == -1);

            var assetPathsDict = new Dictionary<string, DependencyNode>();
            foreach (var assetPath in allPlayerResources)
            {
                if ((File.GetAttributes(assetPath) & FileAttributes.Directory) == FileAttributes.Directory)
                    continue;

                var root = AddResourceAsset(assetPath, assetPathsDict, issues, null);
                var dependencies = AssetDatabase.GetDependencies(assetPath, true);
                foreach (var depAssetPath in dependencies)
                {
                    // skip self
                    if (depAssetPath.Equals(assetPath))
                        continue;

                    AddResourceAsset(depAssetPath, assetPathsDict, issues, root);
                }
            }
        }

        static void AnalyzeStreamingAssets(ProjectAuditorSettings settings, IList<ProjectIssue> issues)
        {
            if (Directory.Exists("Assets/StreamingAssets"))
            {
                long totalBytes = 0;
                string[] files = Directory.GetFiles("Assets/StreamingAssets", "*", SearchOption.AllDirectories);
                foreach (var file in files)
                {
                    var fileInfo = new FileInfo(file);
                    totalBytes += fileInfo.Length;
                }

                if (totalBytes > settings.StreamingAssetsFolderSizeLimit * 1024 * 1024)
                {
                    issues.Add(
                        ProjectIssue.Create(IssueCategory.AssetDiagnostic, k_StreamingAssetsFolderDescriptor,
                            Formatting.FormatSize((ulong)totalBytes))
                    );
                }
            }
        }

        static DependencyNode AddResourceAsset(
            string assetPath, Dictionary<string, DependencyNode> assetPathsDict, IList<ProjectIssue> issues, DependencyNode parent)
        {
            // skip C# scripts
            if (Path.GetExtension(assetPath).Equals(".cs"))
                return null;

            if (assetPathsDict.ContainsKey(assetPath))
            {
                var dep = assetPathsDict[assetPath];
                if (parent != null)
                    dep.AddChild(parent);
                return dep;
            }

            var location = new Location(assetPath);
            var dependencyNode = new AssetDependencyNode
            {
                location = new Location(assetPath)
            };
            if (parent != null)
                dependencyNode.AddChild(parent);

            var isInResources = assetPath.IndexOf("/resources/", StringComparison.OrdinalIgnoreCase) >= 0;

            issues.Add(ProjectIssue.Create
                (
                    IssueCategory.AssetDiagnostic,
                    k_ResourcesFolderDescriptor,
                    Path.GetFileName(assetPath), isInResources ? "is in a Resources folder" : "is a dependency of a Resources folder asset"
                )
                .WithDependencies(dependencyNode)
                .WithLocation(location));

            assetPathsDict.Add(assetPath, dependencyNode);

            return dependencyNode;
        }
    }
}
