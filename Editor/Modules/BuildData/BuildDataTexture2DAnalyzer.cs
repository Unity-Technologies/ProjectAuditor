using System.Collections.Generic;
using Unity.ProjectAuditor.Editor.Core;
using Unity.ProjectAuditor.Editor.Interfaces;

namespace Unity.ProjectAuditor.Editor.Modules
{
    class BuildDataTexture2DAnalyzer : IBuildDataTexture2DModuleAnalyzer
    {
        public void Initialize(Module module)
        {
        }

        public IEnumerable<ProjectIssue> Analyze(BuildDataTexture2DAnalyzerContext context)
        {
            yield return context.CreateInsight(IssueCategory.BuildDataTexture2D, context.Texture.Name)
                .WithCustomProperties(
                    new object[((int)BuildDataTextureProperty.Num)]
                    {
                        context.Texture.Size,
                        context.Texture.Width,
                        context.Texture.Height,
                        context.Texture.Format,
                        context.Texture.MipCount,
                        context.Texture.RwEnabled,
                        context.Texture.BuildFile.DisplayName,
                    });
        }
    }
}