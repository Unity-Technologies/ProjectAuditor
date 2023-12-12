using System.Collections.Generic;
using System.Linq;
using Unity.ProjectAuditor.Editor.Core;
using Unity.ProjectAuditor.Editor.Interfaces;
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
            Category = IssueCategory.BuildDataAnimationClip,
            Properties = new[]
            {
                new PropertyDefinition { Type = PropertyType.Description, Format = PropertyFormat.String, Name = "Name", LongName = "AnimationClip Name" },
                new PropertyDefinition { Type = PropertyTypeUtil.FromCustom(BuildDataAnimationClipProperty.AssetBundle), Format = PropertyFormat.String, Name = "File", LongName = "File Name" },
                new PropertyDefinition { Type = PropertyTypeUtil.FromCustom(BuildDataAnimationClipProperty.Size), Format = PropertyFormat.Bytes, Name = "Size", LongName = "Size" },
                new PropertyDefinition { Type = PropertyTypeUtil.FromCustom(BuildDataAnimationClipProperty.Legacy), Format = PropertyFormat.Bool, Name = "Legacy", LongName = "Legacy" },
                new PropertyDefinition { Type = PropertyTypeUtil.FromCustom(BuildDataAnimationClipProperty.Events), Format = PropertyFormat.Integer, Name = "Events", LongName = "Events" },
            }
        };

        public override string Name => "BuildDataAnimationClips";

        public override IReadOnlyCollection<IssueLayout> SupportedLayouts => new IssueLayout[]
        {
            k_AnimationClipLayout
        };

        public override AnalysisResult Audit(AnalysisParams projectAuditorParams, IProgress progress = null)
        {
            var analyzers = GetPlatformAnalyzers(projectAuditorParams.Platform);

            if (projectAuditorParams.BuildObjects != null)
            {
                var animationClips = projectAuditorParams.BuildObjects.GetObjects<AnimationClip>();

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

            return AnalysisResult.Success;
        }
    }
}
