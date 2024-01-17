using System.Collections.Generic;
using System.IO;
using Unity.ProjectAuditor.Editor.Core;
using Unity.ProjectAuditor.Editor.Diagnostic;
using Unity.ProjectAuditor.Editor.Interfaces;
using Unity.ProjectAuditor.Editor.Utils;
using UnityEditor;

namespace Unity.ProjectAuditor.Editor.SettingsAnalysis
{
    class StreamingAssetsFolderAnalyzer : ISettingsModuleAnalyzer
    {
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

        const string k_StreamingAssetsFolderSizeLimit   = "StreamingAssetsFolderSizeLimit";

        public void Initialize(Module module)
        {
            module.RegisterDescriptor(k_StreamingAssetsFolderDescriptor);
        }

        public void CacheParameters(DiagnosticParams diagnosticParams)
        {
            // settings module analyzers run only once so no need to cache settings parameters
        }

        public void RegisterParameters(DiagnosticParams diagnosticParams)
        {
            diagnosticParams.RegisterParameter(k_StreamingAssetsFolderSizeLimit, 50);
        }

        public IEnumerable<ReportItem> Analyze(SettingsAnalysisContext context)
        {
            // StreamingAssets folder is checked once, AssetsModule might not be the best place this check
            if (k_StreamingAssetsFolderDescriptor.IsApplicable(context.Params))
            {
                var issue = AnalyzeStreamingAssets(context);
                if (issue != null)
                    yield return issue;
            }
        }

        static ReportItem AnalyzeStreamingAssets(AnalysisContext context)
        {
            if (!Directory.Exists("Assets/StreamingAssets"))
                return null;

            var totalBytes = 0L;
            var files = Directory.GetFiles("Assets/StreamingAssets", "*", SearchOption.AllDirectories);
            foreach (var file in files)
            {
                var fileInfo = new FileInfo(file);
                totalBytes += fileInfo.Length;
            }

            var folderSizeLimitMB =
                context.Params.DiagnosticParams.GetParameter(k_StreamingAssetsFolderSizeLimit);

            if (totalBytes <= folderSizeLimitMB * 1024 * 1024)
                return null;

            return context.CreateIssue(IssueCategory.ProjectSetting, k_StreamingAssetsFolderDescriptor.Id,
                Formatting.FormatSize((ulong)totalBytes));
        }
    }
}
