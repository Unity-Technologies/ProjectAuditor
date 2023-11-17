using System.Collections.Generic;
using System.Linq;
using Unity.ProjectAuditor.Editor.Build;
using Unity.ProjectAuditor.Editor.BuildData;
using Unity.ProjectAuditor.Editor.Core;
using Unity.ProjectAuditor.Editor.Interfaces;
using UnityEditor;
using Unity.ProjectAuditor.Editor.UnityFileSystemApi;
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
            category = IssueCategory.BuildDataAudioClip,
            properties = new[]
            {
                new PropertyDefinition { type = PropertyType.Description, format = PropertyFormat.String, name = "Name", longName = "AudioClip Name" },
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(BuildDataAudioClipProperty.AssetBundle), format = PropertyFormat.String, name = "File", longName = "File Name" },
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(BuildDataAudioClipProperty.Size), format = PropertyFormat.Bytes, name = "Size", longName = "Size" },
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(BuildDataAudioClipProperty.BitsPerSample), format = PropertyFormat.Integer, name = "BitsPerSample", longName = "Bits Per Sample" },
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(BuildDataAudioClipProperty.Frequency), format = PropertyFormat.Integer, name = "Frequency", longName = "Frequency" },
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(BuildDataAudioClipProperty.Channels), format = PropertyFormat.Integer, name = "Channels", longName = "Number Of Channels" },
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(BuildDataAudioClipProperty.LoadType), format = PropertyFormat.String, name = "LoadType", longName = "Load Type" },
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(BuildDataAudioClipProperty.Format), format = PropertyFormat.String, name = "Format", longName = "Format" },
            }
        };

        public override string Name => "AudioClipes";

        public override bool IsEnabledByDefault => false;

        public override IReadOnlyCollection<IssueLayout> SupportedLayouts => new IssueLayout[]
        {
            k_AudioClipLayout
        };

        public override void Audit(AnalysisParams projectAuditorParams, IProgress progress = null)
        {
            var analyzers = GetPlatformAnalyzers(projectAuditorParams.Platform);

            if (projectAuditorParams.BuildAnalyzer != null)
            {
                var audioClips = projectAuditorParams.BuildAnalyzer.GetSerializedObjects<AudioClip>();

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

            projectAuditorParams.OnModuleCompleted?.Invoke();
        }
    }
}
