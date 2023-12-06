using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.ProjectAuditor.Editor.Core;
using Unity.ProjectAuditor.Editor.Diagnostic;
using Unity.ProjectAuditor.Editor.Utils;
using UnityEditor;

namespace Unity.ProjectAuditor.Editor.Modules
{
    internal class AssetsModule : Module
    {
        internal static readonly IssueLayout k_IssueLayout = new IssueLayout
        {
            category = IssueCategory.AssetDiagnostic,
            properties = new[]
            {
                new PropertyDefinition { type = PropertyType.Description, name = "Issue", longName = "Issue description"},
                new PropertyDefinition { type = PropertyType.Severity, format = PropertyFormat.String, name = "Severity"},
                new PropertyDefinition { type = PropertyType.Areas, format = PropertyFormat.String, name = "Areas", longName = "Impacted Areas" },
                new PropertyDefinition { type = PropertyType.Path, name = "Path"},
                new PropertyDefinition { type = PropertyType.Descriptor, name = "Descriptor", defaultGroup = true, hidden = true},
            }
        };

        internal const string PAA3000 = nameof(PAA3000);
        internal const string PAA3001 = nameof(PAA3001);

        static readonly Descriptor k_AssetInResourcesFolderDescriptor = new Descriptor
            (
            PAA3000,
            "Resources folder asset & dependencies",
            Areas.BuildSize,
            "The <b>Resources folder</b> is a common source of many problems in Unity projects. Improper use of the Resources folder can bloat the size of a project’s build, lead to uncontrollable excessive memory utilization, and significantly increase application startup times.",
            "Use AssetBundles or Addressables when possible."
            )
        {
            MessageFormat = "'{0}' {1}"
        };

        static readonly Descriptor k_StreamingAssetsFolderDescriptor = new Descriptor(
            PAA3001,
            "StreamingAssets folder size",
            Areas.BuildSize,
            $"There are many files in the <b>StreamingAssets folder</b>. Keeping them in the StreamingAssets folder will increase the build size.",
            $"Try to move files outside this folder and use Asset Bundles or Addressables when possible."
        )
        {
            Platforms = new[] { BuildTarget.Android, BuildTarget.iOS},
            MessageFormat = "StreamingAssets folder contains {0} of data",
        };

        public override string Name => "Assets";

        public override IReadOnlyCollection<IssueLayout> SupportedLayouts => new IssueLayout[] {k_IssueLayout};

        public override void Initialize()
        {
            base.Initialize();

            RegisterDescriptor(k_AssetInResourcesFolderDescriptor);
            RegisterDescriptor(k_StreamingAssetsFolderDescriptor);
        }

        const string k_StreamingAssetsFolderSizeLimit   = "StreamingAssetsFolderSizeLimit";

        public override void RegisterParameters(DiagnosticParams diagnosticParams)
        {
            diagnosticParams.RegisterParameter(k_StreamingAssetsFolderSizeLimit, 50);
        }

        public override AnalysisResult Audit(AnalysisParams analysisParams, IProgress progress = null)
        {
            var context = new AnalysisContext
            {
                Params = analysisParams
            };

            var issues = new List<ProjectIssue>();
            AnalyzeResources(context, issues);

            if (k_StreamingAssetsFolderDescriptor.IsApplicable(analysisParams))
                AnalyzeStreamingAssets(context, issues);

            if (issues.Any())
                analysisParams.OnIncomingIssues(issues);
            return AnalysisResult.Success;
        }

        static void AnalyzeResources(AnalysisContext context, IList<ProjectIssue> issues)
        {
            var allAssetPaths = GetAssetPaths(context);
            var allResources =
                allAssetPaths.Where(path => path.IndexOf("/resources/", StringComparison.OrdinalIgnoreCase) >= 0);
            var allPlayerResources =
                allResources.Where(path => path.IndexOf("/editor/", StringComparison.OrdinalIgnoreCase) == -1);

            var assetPathsDict = new Dictionary<string, DependencyNode>();
            foreach (var assetPath in allPlayerResources)
            {
                if ((File.GetAttributes(assetPath) & FileAttributes.Directory) == FileAttributes.Directory)
                    continue;

                var root = AddResourceAsset(context, assetPath, assetPathsDict, issues, null);
                var dependencies = AssetDatabase.GetDependencies(assetPath, true);
                foreach (var depAssetPath in dependencies)
                {
                    // skip self
                    if (depAssetPath.Equals(assetPath))
                        continue;

                    AddResourceAsset(context, depAssetPath, assetPathsDict, issues, root);
                }
            }
        }

        static void AnalyzeStreamingAssets(AnalysisContext context, IList<ProjectIssue> issues)
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

                var folderSizeLimitMB =
                    context.Params.DiagnosticParams.GetParameter(k_StreamingAssetsFolderSizeLimit);

                if (totalBytes > folderSizeLimitMB * 1024 * 1024)
                {
                    issues.Add(
                        context.CreateIssue(IssueCategory.AssetDiagnostic, k_StreamingAssetsFolderDescriptor.Id,
                            Formatting.FormatSize((ulong)totalBytes))
                    );
                }
            }
        }

        static DependencyNode AddResourceAsset(AnalysisContext context,
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

            issues.Add(context.CreateIssue
                (
                    IssueCategory.AssetDiagnostic,
                    k_AssetInResourcesFolderDescriptor.Id,
                    Path.GetFileName(assetPath), isInResources ? "is in a Resources folder" : "is a dependency of a Resources folder asset"
                )
                .WithDependencies(dependencyNode)
                .WithLocation(location));

            assetPathsDict.Add(assetPath, dependencyNode);

            return dependencyNode;
        }
    }
}
