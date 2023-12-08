using System;
using System.Collections.Generic;
using System.IO;
using Unity.ProjectAuditor.Editor.Core;
using Unity.ProjectAuditor.Editor.Diagnostic;
using Unity.ProjectAuditor.Editor.Interfaces;
using Unity.ProjectAuditor.Editor.Utils;
using UnityEditor;

namespace Unity.ProjectAuditor.Editor.Modules
{
    internal class AssetsModule : ModuleWithAnalyzers<IAssetsModuleAnalyzer>
    {
        internal static readonly IssueLayout k_IssueLayout = new IssueLayout
        {
            category = IssueCategory.AssetDiagnostic,
            properties = new[]
            {
                new PropertyDefinition { type = PropertyType.Description, name = "Issue", longName = "Issue description", maxAutoWidth = 800 },
                new PropertyDefinition { type = PropertyType.Severity, format = PropertyFormat.String, name = "Severity"},
                new PropertyDefinition { type = PropertyType.Areas, format = PropertyFormat.String, name = "Areas", longName = "Impacted Areas" },
                new PropertyDefinition { type = PropertyType.Path, name = "Path", maxAutoWidth = 500 },
                new PropertyDefinition { type = PropertyType.Descriptor, name = "Descriptor", defaultGroup = true, hidden = true},
            }
        };

        internal const string PAA3002 = nameof(PAA3002);

        static readonly Descriptor k_StreamingAssetsFolderDescriptor = new Descriptor(
            PAA3002,
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

            // StreamingAssets folder is checked once, AssetsModule might not be the best place this check
            if (k_StreamingAssetsFolderDescriptor.IsApplicable(analysisParams))
            {
                var issue = AnalyzeStreamingAssets(context);
                if (issue != null)
                    analysisParams.OnIncomingIssues(new[] {issue});
            }

            var analyzers = GetPlatformAnalyzers(analysisParams.Platform);
            if (analyzers.Length == 0)
                return AnalysisResult.Success;

            var allAssetPaths = GetAssetPaths(context);

            progress?.Start("Finding Assets", "Search in Progress...", allAssetPaths.Length);

            foreach (var assetPath in allAssetPaths)
            {
                if (assetPath.IndexOf("/editor/", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    continue;
                }
                if (progress?.IsCancelled ?? false)
                    return AnalysisResult.Cancelled;

                var assetAnalysisContext = new AssetAnalysisContext
                {
                    AssetPath = assetPath,
                    Params = analysisParams
                };

                foreach (var analyzer in analyzers)
                {
                    analysisParams.OnIncomingIssues(analyzer.Analyze(assetAnalysisContext));
                }

                progress?.Advance();
            }

            progress?.Clear();

            return AnalysisResult.Success;
        }

        static ProjectIssue AnalyzeStreamingAssets(AnalysisContext context)
        {
            if (!Directory.Exists("Assets/StreamingAssets"))
                return null;

            long totalBytes = 0;
            string[] files = Directory.GetFiles("Assets/StreamingAssets", "*", SearchOption.AllDirectories);
            foreach (var file in files)
            {
                var fileInfo = new FileInfo(file);
                totalBytes += fileInfo.Length;
            }

            var folderSizeLimitMB =
                context.Params.DiagnosticParams.GetParameter(k_StreamingAssetsFolderSizeLimit);

            if (totalBytes <= folderSizeLimitMB * 1024 * 1024)
                return null;

            return context.CreateIssue(IssueCategory.AssetDiagnostic, k_StreamingAssetsFolderDescriptor.Id,
                Formatting.FormatSize((ulong)totalBytes));
        }
    }
}
