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
            yield return context.CreateWithoutDiagnostic(IssueCategory.BuildDataTexture2D, context.Texture.Name)
                .WithCustomProperties(
                    new object[((int)BuildDataTextureProperty.Num)]
                    {
                        context.Texture.BuildFile.DisplayName,
                        context.Texture.Size,
                        context.Texture.Width,
                        context.Texture.Height,
                        context.Texture.Format.ToString(),
                        context.Texture.MipCount,
                        context.Texture.RwEnabled
                    });
        }
    }
}
