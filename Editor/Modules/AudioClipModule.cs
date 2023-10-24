using System.Collections.Generic;
using UnityEditor;
using Unity.ProjectAuditor.Editor.Core;
using Unity.ProjectAuditor.Editor.Interfaces;

namespace Unity.ProjectAuditor.Editor.Modules
{
    enum AudioClipProperty
    {
        Length = 0,
        SourceFileSize,
        ImportedFileSize,
        RuntimeSize,
        CompressionRatio,
        CompressionFormat,
        SampleRate,
        ForceToMono,
        LoadInBackground,
        PreloadAudioData,
        LoadType,

        Num
    }

    class AudioClipModule : ProjectAuditorModuleWithAnalyzers<IAudioClipModuleAnalyzer>
    {
        static readonly IssueLayout k_AudioClipLayout = new IssueLayout
        {
            category = IssueCategory.AudioClip,
            properties = new[]
            {
                new PropertyDefinition { type = PropertyType.Description, name = "Name" },
                new PropertyDefinition { type = PropertyType.FileType, name = "Format", defaultGroup = true },
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(AudioClipProperty.Length), format = PropertyFormat.String, name = "Length"},
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(AudioClipProperty.SourceFileSize), format = PropertyFormat.Bytes, name = "Source File Size"},
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(AudioClipProperty.ImportedFileSize), format = PropertyFormat.Bytes, name = "Imported File Size"},
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(AudioClipProperty.RuntimeSize), format = PropertyFormat.Bytes, name = "Runtime Size (Estimate)"},
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(AudioClipProperty.CompressionRatio), format = PropertyFormat.String, name = "Compression Ratio"},
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(AudioClipProperty.CompressionFormat), format = PropertyFormat.String, name = "Compression Format"},
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(AudioClipProperty.SampleRate), format = PropertyFormat.String, name = "Sample Rate"},
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(AudioClipProperty.ForceToMono), format = PropertyFormat.Bool, name = "Force To Mono"},
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(AudioClipProperty.LoadInBackground), format = PropertyFormat.Bool, name = "Load In Background"},
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(AudioClipProperty.PreloadAudioData), format = PropertyFormat.Bool, name = "Preload Audio Data" },
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(AudioClipProperty.LoadType), format = PropertyFormat.String, name = "Load Type" },
                new PropertyDefinition { type = PropertyType.Path, name = "Path"}
            }
        };

        public override string name => "AudioClips";

        public override bool isEnabledByDefault => false;

        public override IReadOnlyCollection<IssueLayout> supportedLayouts => new IssueLayout[]
        {
            k_AudioClipLayout,
            AssetsModule.k_IssueLayout
        };

        public override void Audit(ProjectAuditorParams projectAuditorParams, IProgress progress = null)
        {
            var analyzers = GetPlatformAnalyzers(projectAuditorParams.Platform);
            foreach (var analyzer in analyzers)
            {
                analyzer.PrepareForAnalysis(projectAuditorParams);
            }

            var assetPaths = GetAssetPathsByFilter("t:AudioClip, a:assets");

            progress?.Start("Finding AudioClips", "Search in Progress...", assetPaths.Length);

            foreach (var assetPath in assetPaths)
            {
                var audioImporter = AssetImporter.GetAtPath(assetPath) as AudioImporter;
                if (audioImporter == null)
                {
                    continue;
                }

                foreach (var analyzer in analyzers)
                {
                    projectAuditorParams.OnIncomingIssues(analyzer.Analyze(projectAuditorParams, audioImporter));
                }

                progress?.Advance();
            }

            progress?.Clear();

            projectAuditorParams.OnModuleCompleted?.Invoke();
        }
    }
}
