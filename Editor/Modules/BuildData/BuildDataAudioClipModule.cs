using System.Collections.Generic;
using System.Linq;
using Unity.ProjectAuditor.Editor.Core;
using Unity.ProjectAuditor.Editor.Interfaces;
using Unity.ProjectAuditor.Editor.BuildData.SerializedObjects;

namespace Unity.ProjectAuditor.Editor.Modules
{
    enum BuildDataAudioClipProperty
    {
        AssetBundle,
        Size,
        BitsPerSample,
        Frequency,
        Channels,
        LoadType,
        Format,
        Num,
    }

    class BuildDataAudioClipModule : ModuleWithAnalyzers<IBuildDataAudioClipModuleAnalyzer>
    {
        static readonly IssueLayout k_AudioClipLayout = new IssueLayout
        {
            Category = IssueCategory.BuildDataAudioClip,
            Properties = new[]
            {
                new PropertyDefinition { Type = PropertyType.Description, Format = PropertyFormat.String, Name = "Name", LongName = "AudioClip Name" },
                new PropertyDefinition { Type = PropertyTypeUtil.FromCustom(BuildDataAudioClipProperty.AssetBundle), Format = PropertyFormat.String, Name = "File", LongName = "File Name" },
                new PropertyDefinition { Type = PropertyTypeUtil.FromCustom(BuildDataAudioClipProperty.Size), Format = PropertyFormat.Bytes, Name = "Size", LongName = "Size" },
                new PropertyDefinition { Type = PropertyTypeUtil.FromCustom(BuildDataAudioClipProperty.BitsPerSample), Format = PropertyFormat.Integer, Name = "BitsPerSample", LongName = "Bits Per Sample" },
                new PropertyDefinition { Type = PropertyTypeUtil.FromCustom(BuildDataAudioClipProperty.Frequency), Format = PropertyFormat.Integer, Name = "Frequency", LongName = "Frequency" },
                new PropertyDefinition { Type = PropertyTypeUtil.FromCustom(BuildDataAudioClipProperty.Channels), Format = PropertyFormat.Integer, Name = "Channels", LongName = "Number Of Channels" },
                new PropertyDefinition { Type = PropertyTypeUtil.FromCustom(BuildDataAudioClipProperty.LoadType), Format = PropertyFormat.String, Name = "LoadType", LongName = "Load Type" },
                new PropertyDefinition { Type = PropertyTypeUtil.FromCustom(BuildDataAudioClipProperty.Format), Format = PropertyFormat.String, Name = "Format", LongName = "Format" },
            }
        };

        public override string Name => "AudioClipes";

        public override IReadOnlyCollection<IssueLayout> SupportedLayouts => new IssueLayout[]
        {
            k_AudioClipLayout
        };

        public override AnalysisResult Audit(AnalysisParams projectAuditorParams, IProgress progress = null)
        {
            var analyzers = GetPlatformAnalyzers(projectAuditorParams.Platform);

            if (projectAuditorParams.BuildObjects != null)
            {
                var audioClips = projectAuditorParams.BuildObjects.GetObjects<AudioClip>();

                progress?.Start("Parsing AudioClips from Build Data", "Search in Progress...", audioClips.Count());

                foreach (var audioClip in audioClips)
                {
                    foreach (var analyzer in analyzers)
                    {
                        var context = new BuildDataAudioClipAnalyzerContext
                        {
                            AudioClip = audioClip,
                            Params = projectAuditorParams
                        };

                        projectAuditorParams.OnIncomingIssues(analyzer.Analyze(context));
                    }

                    progress?.Advance();
                }

                progress?.Clear();
            }

            return AnalysisResult.Success;
        }
    }
}
