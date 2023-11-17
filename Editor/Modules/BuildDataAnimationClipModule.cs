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
    enum BuildDataAnimationClipProperty
    {
        AssetBundle,
        Size,
        Legacy,
        Events,
        Num,
    }

    class BuildDataAnimationClipModule : ModuleWithAnalyzers<IBuildDataAnimationClipModuleAnalyzer>
    {
        static readonly IssueLayout k_AnimationClipLayout = new IssueLayout
        {
            category = IssueCategory.BuildDataAnimationClip,
            properties = new[]
            {
                new PropertyDefinition { type = PropertyType.Description, format = PropertyFormat.String, name = "Name", longName = "AnimationClip Name" },
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(BuildDataAnimationClipProperty.AssetBundle), format = PropertyFormat.String, name = "File", longName = "File Name" },
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(BuildDataAnimationClipProperty.Size), format = PropertyFormat.Bytes, name = "Size", longName = "Size" },
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(BuildDataAnimationClipProperty.Legacy), format = PropertyFormat.Bool, name = "Legacy", longName = "Legacy" },
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(BuildDataAnimationClipProperty.Events), format = PropertyFormat.Integer, name = "Events", longName = "Events" },
            }
        };

        public override string Name => "AnimationClipes";

        public override bool IsEnabledByDefault => false;

        public override IReadOnlyCollection<IssueLayout> SupportedLayouts => new IssueLayout[]
        {
            k_AnimationClipLayout
        };

        public override void Audit(AnalysisParams projectAuditorParams, IProgress progress = null)
        {
            var analyzers = GetPlatformAnalyzers(projectAuditorParams.Platform);

            if (projectAuditorParams.BuildAnalyzer != null)
            {
                var animationClips = projectAuditorParams.BuildAnalyzer.GetSerializedObjects<AnimationClip>();

                progress?.Start("Parsing AnimationClips from Build Data", "Search in Progress...", animationClips.Count());

                foreach (var animationClip in animationClips)
                {
                    foreach (var analyzer in analyzers)
                    {
                        var context = new BuildDataAnimationClipAnalyzerContext
                        {
                            AnimationClip = animationClip,
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
