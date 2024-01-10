using System.Collections.Generic;
using Unity.ProjectAuditor.Editor.Core;
using Unity.ProjectAuditor.Editor.Interfaces;

namespace Unity.ProjectAuditor.Editor.Modules
{
    class BuildDataAnimationClipAnalyzer : IBuildDataAnimationClipModuleAnalyzer
    {
        public void Initialize(Module module)
        {
        }

        public IEnumerable<ProjectIssue> Analyze(BuildDataAnimationClipAnalyzerContext context)
        {
            yield return context.CreateInsight(IssueCategory.BuildDataAnimationClip, context.AnimationClip.Name)
                .WithCustomProperties(
                    new object[((int)BuildDataAnimationClipProperty.Num)]
                    {
                        context.AnimationClip.Size,
                        context.AnimationClip.Legacy,
                        context.AnimationClip.Events,
                        context.AnimationClip.BuildFile.DisplayName,
                    });
        }
    }
}
