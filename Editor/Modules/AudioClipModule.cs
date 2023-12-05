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

    class AudioClipModule : ModuleWithAnalyzers<IAudioClipModuleAnalyzer>
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

        public override string Name => "AudioClips";

        public override bool IsEnabledByDefault => false;

        public override IReadOnlyCollection<IssueLayout> SupportedLayouts => new IssueLayout[]
        {
            k_AudioClipLayout,
            AssetsModule.k_IssueLayout
        };

        const string k_StreamingClipThresholdBytes            = "StreamingClipThresholdBytes";
        const string k_LongDecompressedClipThresholdBytes     = "LongDecompressedClipThresholdBytes";
        const string k_LongCompressedMobileClipThresholdBytes = "LongCompressedMobileClipThresholdBytes";
        const string k_LoadInBackGroundClipSizeThresholdBytes = "LoadInBackGroundClipSizeThresholdBytes";

        public override void RegisterParameters(DiagnosticParams diagnosticParams)
        {
            diagnosticParams.RegisterParameter(k_StreamingClipThresholdBytes, 1 * (64000 + (int)(1.6 * 48000 * 2)) + 694);
            diagnosticParams.RegisterParameter(k_LongDecompressedClipThresholdBytes, 200 * 1024);
            diagnosticParams.RegisterParameter(k_LongCompressedMobileClipThresholdBytes, 200 * 1024);
            diagnosticParams.RegisterParameter(k_LoadInBackGroundClipSizeThresholdBytes, 200 * 1024);
        }

        public override void Audit(AnalysisParams analysisParams, IProgress progress = null)
        {
            var analyzers = GetPlatformAnalyzers(analysisParams.Platform);

            var diagnosticParams = analysisParams.DiagnosticParams;

            var context = new AudioClipAnalysisContext
            {
                // Importer is set in the loop
                Params = analysisParams,
                StreamingClipThresholdBytes = diagnosticParams.GetParameter(k_StreamingClipThresholdBytes),
                LongDecompressedClipThresholdBytes = diagnosticParams.GetParameter(k_LongDecompressedClipThresholdBytes),
                LongCompressedMobileClipThresholdBytes = diagnosticParams.GetParameter(k_LongCompressedMobileClipThresholdBytes),
                LoadInBackGroundClipSizeThresholdBytes = diagnosticParams.GetParameter(k_LoadInBackGroundClipSizeThresholdBytes)
            };

            var assetPaths = GetAssetPathsByFilter("t:AudioClip, a:assets", context);

            progress?.Start("Finding AudioClips", "Search in Progress...", assetPaths.Length);

            foreach (var assetPath in assetPaths)
            {
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

            analysisParams.OnModuleCompleted?.Invoke();
        }
    }
}
