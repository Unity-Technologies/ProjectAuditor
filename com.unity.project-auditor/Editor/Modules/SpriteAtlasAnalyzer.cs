using System;
using System.Collections.Generic;
using Unity.ProjectAuditor.Editor.Core;
using Unity.ProjectAuditor.Editor.Utils;
using UnityEditor;
using UnityEngine.U2D;

namespace Unity.ProjectAuditor.Editor.Modules
{
    internal class SpriteAtlasAnalyzer : SpriteAtlasModuleAnalyzer
    {
        internal const string PAA0008 = nameof(PAA0008);

        internal static readonly Descriptor k_PoorUtilizationDescriptor = new Descriptor(
            PAA0008,
            "Sprite Atlas: Too much empty space",
            Areas.Memory,
            "The Sprite Atlas texture contains a lot of empty space. Empty space contributes to texture memory usage.",
            "Consider reorganizing your Sprite Atlas Texture in order to reduce the amount of empty space."
        )
        {
            IsEnabledByDefault = false,
            MessageFormat = "Sprite Atlas '{0}' has too much empty space ({1})"
        };

        [DiagnosticParameter("SpriteAtlasEmptySpaceLimit", 50)]
        int m_EmptySpaceLimit;

        public override void Initialize(Action<Descriptor> registerDescriptor)
        {
            registerDescriptor(k_PoorUtilizationDescriptor);
        }

        public override IEnumerable<ReportItem> Analyze(SpriteAtlasAnalysisContext context)
        {
            if (context.IsDescriptorEnabled(k_PoorUtilizationDescriptor))
            {
                var spriteAtlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(context.AssetPath);
                var emptySpace = TextureUtils.GetEmptySpacePercentage(spriteAtlas);
                if (emptySpace > m_EmptySpaceLimit)
                {
                    yield return context.CreateIssue(IssueCategory.AssetIssue,
                        k_PoorUtilizationDescriptor.Id, spriteAtlas.name, Formatting.FormatPercentage(emptySpace / 100.0f))
                        .WithLocation(context.AssetPath);
                }
            }
        }
    }
}
