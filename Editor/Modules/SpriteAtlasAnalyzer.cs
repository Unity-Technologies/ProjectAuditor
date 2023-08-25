using System.Collections.Generic;
using System.IO;
using Unity.ProjectAuditor.Editor.Core;
using Unity.ProjectAuditor.Editor.Diagnostic;
using Unity.ProjectAuditor.Editor.Interfaces;
using Unity.ProjectAuditor.Editor.Utils;
using UnityEditor;
using UnityEngine.U2D;

namespace Unity.ProjectAuditor.Editor.Modules
{
    internal class SpriteAtlasAnalyzer : ISpriteAtlasModuleAnalyzer
    {
        internal const string PAA0006 = nameof(PAA0006);

        internal static readonly Descriptor k_SpriteAtlasEmptyDescriptor = new Descriptor(
            PAA0006,
            "Sprite Atlas: Too much empty space",
            new[] {Area.Memory},
            "The Sprite Atlas texture contains a lot of empty space. Empty space contributes to texture memory usage.",
            "Consider reorganizing your Sprite Atlas Texture in order to reduce the amount of empty space."
        )
        {
            messageFormat = "Sprite Atlas '{0}' has too much empty space ({1}%)"
        };

        public void Initialize(ProjectAuditorModule module)
        {
            module.RegisterDescriptor(k_SpriteAtlasEmptyDescriptor);
        }

        public IEnumerable<ProjectIssue> Analyze(ProjectAuditorParams projectAuditorParams, string assetPath)
        {
            var spriteAtlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(assetPath);

            yield return ProjectIssue.Create(IssueCategory.SpriteAtlas, spriteAtlas.name)
                .WithLocation(new Location(assetPath));

            var emptyPercent = TextureUtils.GetEmptySpacePercentage(spriteAtlas);
            if (emptyPercent > projectAuditorParams.diagnosticParams.SpriteAtlasEmptySpaceLimit)
            {
                yield return ProjectIssue.Create(IssueCategory.AssetDiagnostic,
                    k_SpriteAtlasEmptyDescriptor, spriteAtlas.name, emptyPercent)
                    .WithLocation(assetPath);
            }
        }
    }
}
