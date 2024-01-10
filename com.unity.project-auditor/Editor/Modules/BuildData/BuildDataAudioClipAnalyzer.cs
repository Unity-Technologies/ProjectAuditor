using System.Collections.Generic;
using Unity.ProjectAuditor.Editor.Core;
using Unity.ProjectAuditor.Editor.Interfaces;

namespace Unity.ProjectAuditor.Editor.Modules
{
    class BuildDataAudioClipAnalyzer : IBuildDataAudioClipModuleAnalyzer
    {
        public void Initialize(Module module)
        {
        }

        public IEnumerable<ProjectIssue> Analyze(BuildDataAudioClipAnalyzerContext context)
        {
            yield return context.CreateInsight(IssueCategory.BuildDataAudioClip, context.AudioClip.Name)
                .WithCustomProperties(
                    new object[((int)BuildDataAudioClipProperty.Num)]
                    {
                        context.AudioClip.Size,
                        context.AudioClip.BitsPerSample,
                        context.AudioClip.Frequency,
                        context.AudioClip.Channels,
                        context.AudioClip.LoadType,
                        context.AudioClip.Format,
                        context.AudioClip.BuildFile.DisplayName,
                    });
        }
    }
}
