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

    class AudioClipModule : ModuleWithAnalyzers<AudioClipModuleAnalyzer>
    {
        static readonly IssueLayout k_AudioClipLayout = new IssueLayout
        {
            Category = IssueCategory.AudioClip,
            Properties = new[]
            {
                new PropertyDefinition { Type = PropertyType.Description, Name = "Name", MaxAutoWidth = 500 },
                new PropertyDefinition { Type = PropertyType.FileType, Name = "Format", IsDefaultGroup = true },
                new PropertyDefinition { Type = PropertyTypeUtil.FromCustom(AudioClipProperty.Length), Format = PropertyFormat.String, Name = "Length"},
                new PropertyDefinition { Type = PropertyTypeUtil.FromCustom(AudioClipProperty.SourceFileSize), Format = PropertyFormat.Bytes, Name = "Source File Size"},
                new PropertyDefinition { Type = PropertyTypeUtil.FromCustom(AudioClipProperty.ImportedFileSize), Format = PropertyFormat.Bytes, Name = "Imported File Size"},
                new PropertyDefinition { Type = PropertyTypeUtil.FromCustom(AudioClipProperty.RuntimeSize), Format = PropertyFormat.Bytes, Name = "Runtime Size (Estimate)"},
                new PropertyDefinition { Type = PropertyTypeUtil.FromCustom(AudioClipProperty.CompressionRatio), Format = PropertyFormat.String, Name = "Compression Ratio"},
                new PropertyDefinition { Type = PropertyTypeUtil.FromCustom(AudioClipProperty.CompressionFormat), Format = PropertyFormat.String, Name = "Compression Format"},
                new PropertyDefinition { Type = PropertyTypeUtil.FromCustom(AudioClipProperty.SampleRate), Format = PropertyFormat.String, Name = "Sample Rate"},
                new PropertyDefinition { Type = PropertyTypeUtil.FromCustom(AudioClipProperty.ForceToMono), Format = PropertyFormat.Bool, Name = "Force To Mono"},
                new PropertyDefinition { Type = PropertyTypeUtil.FromCustom(AudioClipProperty.LoadInBackground), Format = PropertyFormat.Bool, Name = "Load In Background"},
                new PropertyDefinition { Type = PropertyTypeUtil.FromCustom(AudioClipProperty.PreloadAudioData), Format = PropertyFormat.Bool, Name = "Preload Audio Data" },
                new PropertyDefinition { Type = PropertyTypeUtil.FromCustom(AudioClipProperty.LoadType), Format = PropertyFormat.String, Name = "Load Type" },
                new PropertyDefinition { Type = PropertyType.Path, Name = "Path", MaxAutoWidth = 500 }
            }
        };

        public override string Name => "AudioClips";

        public override IReadOnlyCollection<IssueLayout> SupportedLayouts => new IssueLayout[]
        {
            k_AudioClipLayout,
            AssetsModule.k_IssueLayout
        };

        public override AnalysisResult Audit(AnalysisParams analysisParams, IProgress progress = null)
        {
            var analyzers = GetCompatibleAnalyzers(analysisParams);

            var context = new AudioClipAnalysisContext
            {
                // Importer is set in the loop
                Params = analysisParams
            };

            var assetPaths = GetAssetPathsByFilter("t:AudioClip, a:assets", context);

            progress?.Start("Finding AudioClips", "Search in Progress...", assetPaths.Length);

            foreach (var assetPath in assetPaths)
            {
                // Check if the operation was cancelled
                if (progress?.IsCancelled ?? false)
                    return AnalysisResult.Cancelled;

                var audioImporter = AssetImporter.GetAtPath(assetPath) as AudioImporter;
                if (audioImporter == null)
                {
                    continue;
                }

                context.Importer = audioImporter;
                foreach (var analyzer in analyzers)
                {
                    analysisParams.OnIncomingIssues(analyzer.Analyze(context));
                }

                progress?.Advance();
            }

            progress?.Clear();

            return AnalysisResult.Success;
        }
    }
}
