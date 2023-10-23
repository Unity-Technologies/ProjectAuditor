using System.Collections.Generic;
using System.IO;
using Unity.ProjectAuditor.Editor.Core;
using Unity.ProjectAuditor.Editor.Diagnostic;
using Unity.ProjectAuditor.Editor.Interfaces;
using Unity.ProjectAuditor.Editor.Utils;
using UnityEditor;
using UnityEngine;

namespace Unity.ProjectAuditor.Editor.Modules
{
    class TextureUtilizationAnalyzer : ITextureModuleAnalyzer
    {
        internal const string PAA0005 = nameof(PAA0005);
        internal const string PAA0006 = nameof(PAA0006);
        internal const string PAA0007 = nameof(PAA0007);

        internal static readonly Descriptor k_TextureSolidColorDescriptor = new Descriptor(
            PAA0005,
            "Texture: Solid color is not 1x1 size",
            new[] {Area.Memory},
            "The texture is a single, solid color and is bigger than 1x1 pixels in size. Redundant texture data occupies memory unneccesarily.",
            "Consider shrinking the texture to 1x1 size."
        )
        {
            messageFormat = "Texture '{0}' is a solid color and not 1x1 size",
            fixer = (issue) => { ShrinkSolidTexture(issue.relativePath); }
        };

        // NOTE:  This is only here to run the same analysis without a quick fix button.  Clean up when we either have appropriate quick fix for other dimensions or improved fixer support.
        internal static readonly Descriptor k_TextureSolidColorNoFixerDescriptor = new Descriptor(
            PAA0006,
            "Texture: Solid color is not 1x1 size",
            new[] { Area.Memory },
            "The texture is a single, solid color and is bigger than 1x1 pixels in size. Redundant texture data occupies memory unneccesarily.",
            "Consider shrinking the texture to 1x1 size."
        )
        {
            messageFormat = "Texture '{0}' is a solid color and not 1x1 size"
        };

        internal static readonly Descriptor k_TextureAtlasEmptyDescriptor = new Descriptor(
            PAA0007,
            "Texture Atlas: Too much empty space",
            new[] {Area.Memory},
            "The texture atlas contains a lot of empty space. Empty space contributes to texture memory usage.",
            "Consider reorganizing your texture atlas in order to reduce the amount of empty space."
        )
        {
            messageFormat = "Texture Atlas '{0}' has too much empty space ({1})"
        };

        public void Initialize(ProjectAuditorModule module)
        {
            module.RegisterDescriptor(k_TextureSolidColorDescriptor);
            module.RegisterDescriptor(k_TextureSolidColorNoFixerDescriptor);
            module.RegisterDescriptor(k_TextureAtlasEmptyDescriptor);
        }

        public IEnumerable<ProjectIssue> Analyze(TextureAnalysisContext analysisContext)
        {
            if (TextureUtils.IsTextureSolidColorTooBig(analysisContext.Importer, analysisContext.Texture))
            {
                var dimensionAppropriateDescriptor = analysisContext.Texture.dimension == UnityEngine.Rendering.TextureDimension.Tex2D ? k_TextureSolidColorDescriptor : k_TextureSolidColorNoFixerDescriptor;
                yield return ProjectIssue.Create(IssueCategory.AssetDiagnostic, dimensionAppropriateDescriptor.id, analysisContext.Name)
                    .WithLocation(analysisContext.Importer.assetPath);
            }

            var texture2D = analysisContext.Texture as Texture2D;
            if (texture2D != null)
            {
                var emptyPercent = TextureUtils.GetEmptyPixelsPercent(texture2D);
                if (emptyPercent >
                    analysisContext.Params.DiagnosticParams.SpriteAtlasEmptySpaceLimit)
                {
                    yield return ProjectIssue.Create(IssueCategory.AssetDiagnostic, k_TextureAtlasEmptyDescriptor.id, analysisContext.Name, Formatting.FormatPercentage(emptyPercent / 100.0f))
                        .WithLocation(analysisContext.Importer.assetPath);
                }
            }
        }

        internal static void ShrinkSolidTexture(string path)
        {
            var textureImporter = AssetImporter.GetAtPath(path) as TextureImporter;
            if (textureImporter != null)
            {
                var originalValue = textureImporter.isReadable;
                textureImporter.isReadable = true;
                textureImporter.SaveAndReimport();

                var texture = AssetDatabase.LoadAssetAtPath<Texture>(path) as Texture2D;
                var color = texture.GetPixel(0, 0);
                //Create a new texture as we can't resize the current one
                var newTexture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
                newTexture.SetPixel(0, 0, color);
                newTexture.Apply();

                var pixels = newTexture.EncodeToPNG();
                File.WriteAllBytes(path, pixels);
                AssetDatabase.Refresh();

                textureImporter.isReadable = originalValue;
                textureImporter.SaveAndReimport();
            }
        }
    }
}
