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
        internal const string PAA0008 = nameof(PAA0008);

        internal static readonly Descriptor k_SpriteAtlasEmptyDescriptor = new Descriptor(
            PAA0008,
            "Sprite Atlas: Too much empty space",
            new[] {Area.Memory},
            "The Sprite Atlas texture contains a lot of empty space. Empty space contributes to texture memory usage.",
            "Consider reorganizing your Sprite Atlas Texture in order to reduce the amount of empty space."
        )
        {
            MessageFormat = "Sprite Atlas '{0}' has too much empty space ({1})"
        };

        public void Initialize(Module module)
        {
            module.RegisterDescriptor(k_SpriteAtlasEmptyDescriptor);
        }

        public IEnumerable<ProjectIssue> Analyze(SpriteAtlasAnalysisContext context)
        {
            var spriteAtlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(context.AssetPath);

            yield return context.CreateWithoutDiagnostic(IssueCategory.SpriteAtlas, spriteAtlas.name)
                .WithLocation(new Location(context.AssetPath));

            var emptyPercent = TextureUtils.GetEmptySpacePercentage(spriteAtlas);
            if (emptyPercent > context.SpriteAtlasEmptySpaceLimit)
            {
                yield return context.Create(IssueCategory.AssetDiagnostic,
                    k_SpriteAtlasEmptyDescriptor.Id, spriteAtlas.name, Formatting.FormatPercentage(emptyPercent / 100.0f, 0))
                    .WithLocation(context.AssetPath);
            }
        }
    }
}
