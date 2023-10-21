using System.Collections.Generic;
using System.IO;
using Unity.ProjectAuditor.Editor.Core;
using Unity.ProjectAuditor.Editor.Diagnostic;
using Unity.ProjectAuditor.Editor.Interfaces;
using Unity.ProjectAuditor.Editor.Utils;
using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;

namespace Unity.ProjectAuditor.Editor.Modules
{
    class TextureAnalyzer : ITextureModuleAnalyzer
    {
        internal const string PAA0000 = nameof(PAA0000);
        internal const string PAA0001 = nameof(PAA0001);
        internal const string PAA0002 = nameof(PAA0002);
        internal const string PAA0003 = nameof(PAA0003);
        internal const string PAA0004 = nameof(PAA0004);
        internal const string PAA0005 = nameof(PAA0005);
        internal const string PAA0006 = nameof(PAA0006);
        internal const string PAA0007 = nameof(PAA0007);

        internal static readonly Descriptor k_TextureMipMapNotEnabledDescriptor = new Descriptor(
            PAA0000,
            "Texture: Mipmaps not enabled",
            new[] {Area.GPU, Area.Quality},
            "<b>Generate Mip Maps</b> in the Texture Import Settings is not enabled. Using textures that are not mipmapped in a 3D environment can impact rendering performance and introduce aliasing artifacts.",
            "Consider enabling mipmaps using the <b>Advanced ➔ Generate Mip Maps</b> option in the Texture Import Settings."
        )
        {
            messageFormat = "Texture '{0}' mipmaps generation is not enabled",
            fixer = (issue) =>
            {
                var textureImporter = AssetImporter.GetAtPath(issue.relativePath) as TextureImporter;
                if (textureImporter != null)
                {
                    textureImporter.mipmapEnabled = true;
                    textureImporter.SaveAndReimport();
                }
            }
        };

        internal static readonly Descriptor k_TextureMipMapEnabledDescriptor = new Descriptor(
            PAA0001,
            "Texture: Mipmaps enabled on Sprite/UI texture",
            new[] {Area.BuildSize, Area.Quality},
            "<b>Generate Mip Maps</b> is enabled in the Texture Import Settings for a Sprite/UI texture. This might reduce rendering quality of sprites and UI.",
            "Consider disabling mipmaps using the <b>Advanced ➔ Generate Mip Maps</b> option in the texture inspector. This will also reduce your build size."
        )
        {
            messageFormat = "Texture '{0}' mipmaps generation is enabled",
            fixer = (issue) =>
            {
                var textureImporter = AssetImporter.GetAtPath(issue.relativePath) as TextureImporter;
                if (textureImporter != null)
                {
                    textureImporter.mipmapEnabled = false;
                    textureImporter.SaveAndReimport();
                }
            }
        };

        internal static readonly Descriptor k_TextureReadWriteEnabledDescriptor = new Descriptor(
            PAA0002,
            "Texture: Read/Write enabled",
            Area.Memory,
            "The <b>Read/Write Enabled</b> flag in the Texture Import Settings is enabled. This causes the texture data to be duplicated in memory.",
            "If not required, disable the <b>Read/Write Enabled</b> option in the Texture Import Settings."
        )
        {
            messageFormat = "Texture '{0}' Read/Write is enabled",
            documentationUrl = "https://docs.unity3d.com/Manual/class-TextureImporter.html",
            fixer = (issue) =>
            {
                var textureImporter = AssetImporter.GetAtPath(issue.relativePath) as TextureImporter;
                if (textureImporter != null)
                {
                    textureImporter.isReadable = false;
                    textureImporter.SaveAndReimport();
                }
            }
        };

        internal static readonly Descriptor k_TextureStreamingMipMapEnabledDescriptor = new Descriptor(
            PAA0003,
            "Texture: Mipmaps Streaming not enabled",
            new[] {Area.Memory, Area.Quality},
            "The <b>Streaming Mipmaps</b> option in the Texture Import Settings is not enabled. As a result, all mip levels for this texture are loaded into GPU memory for as long as the texture is loaded, potentially resulting in excessive texture memory usage.",
            "Consider enabling the <b>Streaming Mipmaps</b> option in the Texture Import Settings."
        )
        {
            messageFormat = "Texture '{0}' mipmaps streaming is not enabled",
            fixer = (issue) =>
            {
                var textureImporter = AssetImporter.GetAtPath(issue.relativePath) as TextureImporter;
                if (textureImporter != null)
                {
                    textureImporter.streamingMipmaps = true;
                    textureImporter.SaveAndReimport();
                }
            }
        };

        internal static readonly Descriptor k_TextureAnisotropicLevelDescriptor = new Descriptor(
            PAA0004,
            "Texture: Anisotropic level is higher than 1",
            new[] {Area.GPU, Area.Quality},
            "The <b>Anisotropic Level</b> in the Texture Import Settings is higher than 1. Anisotropic filtering makes textures look better when viewed at a shallow angle, but it can be slower to process on the GPU.",
            "Consider setting the <b>Anisotropic Level</b> to 1."
        )
        {
            platforms = new[] {"Android", "iOS", "Switch"},
            messageFormat = "Texture '{0}' anisotropic level is set to '{1}'",
            fixer = (issue) =>
            {
                var textureImporter = AssetImporter.GetAtPath(issue.relativePath) as TextureImporter;
                if (textureImporter != null)
                {
                    textureImporter.anisoLevel = 1;
                    textureImporter.SaveAndReimport();
                }
            }
        };

        internal static readonly Descriptor k_TextureSolidColorDescriptor = new Descriptor(
            PAA0005,
            "Texture: Solid color is not 1x1 size",
            new[] {Area.Memory},
            "The texture is a single, solid color and is bigger than 1x1 pixels in size. Redundant texture data occupies memory unneccesarily.",
            "Consider shrinking the texture to 1x1 size."
        )
        {
            messageFormat = "Texture '{0}' is a solid color and not 1x1 size",
            fixer = (issue) => { ResizeSolidTexture(issue.relativePath); }
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

        string m_PlatformString;
        int m_TextureSizeLimit;
        int m_TextureStreamingMipmapsSizeLimit;
        private int m_SpriteAtlasEmptySpaceLimit;

        public void Initialize(ProjectAuditorModule module)
        {
            module.RegisterDescriptor(k_TextureMipMapNotEnabledDescriptor);
            module.RegisterDescriptor(k_TextureMipMapEnabledDescriptor);
            module.RegisterDescriptor(k_TextureReadWriteEnabledDescriptor);
            module.RegisterDescriptor(k_TextureStreamingMipMapEnabledDescriptor);
            module.RegisterDescriptor(k_TextureAnisotropicLevelDescriptor);
            module.RegisterDescriptor(k_TextureSolidColorDescriptor);
            module.RegisterDescriptor(k_TextureSolidColorNoFixerDescriptor);
            module.RegisterDescriptor(k_TextureAtlasEmptyDescriptor);
        }

        public void PrepareForAnalysis(ProjectAuditorParams projectAuditorParams)
        {
            m_PlatformString = projectAuditorParams.Platform.ToString();
            var rules = projectAuditorParams.Rules;
            m_TextureSizeLimit = rules.GetParameter("TextureSizeLimit", 2048);
            m_TextureStreamingMipmapsSizeLimit = rules.GetParameter("TextureStreamingMipmapsSizeLimit", 4000);
            m_SpriteAtlasEmptySpaceLimit = rules.GetParameter("SpriteAtlasEmptySpaceLimit", 50);
        }

        public IEnumerable<ProjectIssue> Analyze(ProjectAuditorParams projectAuditorParams, TextureImporter textureImporter, TextureImporterPlatformSettings platformSettings)
        {
            var assetPath = textureImporter.assetPath;

            var texture = AssetDatabase.LoadAssetAtPath<Texture>(assetPath);
#if PA_CAN_USE_COMPUTEMIPCHAINSIZE
            TextureFormat format = (TextureFormat)platformSettings.format;
            if (platformSettings.format == TextureImporterFormat.Automatic)
            {
                format = (TextureFormat)textureImporter.GetAutomaticFormat(m_PlatformString);
            }

            var size = UnityEngine.Experimental.Rendering.GraphicsFormatUtility.ComputeMipChainSize(texture.width, texture.height, TextureUtils.GetTextureDepth(texture), format, texture.mipmapCount);
#else
            // This is not the correct size but we don't have access to the appropriate functionality on older versions to do much better without a lot more work.
            var size = Profiler.GetRuntimeMemorySizeLong(texture);
#endif
            var resolution = texture.width + "x" + texture.height;

            yield return ProjectIssue.CreateWithoutDiagnostic(IssueCategory.Texture, texture.name)
                .WithCustomProperties(
                    new object[(int)TextureProperty.Num]
                    {
                        textureImporter.textureShape,
                        textureImporter.textureType,
                        platformSettings.format,
                        platformSettings.textureCompression,
                        textureImporter.mipmapEnabled,
                        textureImporter.isReadable,
                        resolution,
                        size,
                        textureImporter.streamingMipmaps
                    })
                .WithLocation(new Location(assetPath));

            // diagnostics
            var textureName = Path.GetFileNameWithoutExtension(assetPath);

            if (!textureImporter.mipmapEnabled && textureImporter.textureType == TextureImporterType.Default)
            {
                yield return ProjectIssue.Create(IssueCategory.AssetDiagnostic,
                    k_TextureMipMapNotEnabledDescriptor.id, textureName)
                    .WithLocation(assetPath);
            }

            if (textureImporter.mipmapEnabled &&
                (textureImporter.textureType == TextureImporterType.Sprite || textureImporter.textureType == TextureImporterType.GUI)
            )
            {
                yield return ProjectIssue.Create(IssueCategory.AssetDiagnostic,
                    k_TextureMipMapEnabledDescriptor.id, textureName)
                    .WithLocation(assetPath);
            }

            if (textureImporter.isReadable)
            {
                yield return ProjectIssue.Create(IssueCategory.AssetDiagnostic, k_TextureReadWriteEnabledDescriptor.id, textureName)
                    .WithLocation(textureImporter.assetPath);
            }

            if (textureImporter.mipmapEnabled && !textureImporter.streamingMipmaps && size > Mathf.Pow(m_TextureStreamingMipmapsSizeLimit, 2))
            {
                yield return ProjectIssue.Create(IssueCategory.AssetDiagnostic, k_TextureStreamingMipMapEnabledDescriptor.id, textureName)
                    .WithLocation(textureImporter.assetPath);
            }

            if (k_TextureAnisotropicLevelDescriptor.IsApplicable(projectAuditorParams) &&
                textureImporter.mipmapEnabled && textureImporter.filterMode != FilterMode.Point && textureImporter.anisoLevel > 1)
            {
                yield return ProjectIssue.Create(IssueCategory.AssetDiagnostic, k_TextureAnisotropicLevelDescriptor.id, textureName, textureImporter.anisoLevel)
                    .WithLocation(textureImporter.assetPath);
            }

            if (TextureUtils.IsTextureSolidColorTooBig(textureImporter, texture))
            {
                var dimensionAppropriateDescriptor = texture.dimension == UnityEngine.Rendering.TextureDimension.Tex2D ? k_TextureSolidColorDescriptor : k_TextureSolidColorNoFixerDescriptor;
                yield return ProjectIssue.Create(IssueCategory.AssetDiagnostic, dimensionAppropriateDescriptor.id, textureName)
                    .WithLocation(textureImporter.assetPath);
            }

            var texture2D = texture as Texture2D;
            if (texture2D != null)
            {
                var emptyPercent = TextureUtils.GetEmptyPixelsPercent(texture2D);
                if (emptyPercent > m_SpriteAtlasEmptySpaceLimit)
                {
                    yield return ProjectIssue.Create(IssueCategory.AssetDiagnostic, k_TextureAtlasEmptyDescriptor.id, textureName, Formatting.FormatPercentage(emptyPercent / 100.0f))
                        .WithLocation(textureImporter.assetPath);
                }
            }
        }

        internal static void ResizeSolidTexture(string path)
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

                byte[] pixels = newTexture.EncodeToPNG();
                File.WriteAllBytes(path, pixels);
                AssetDatabase.Refresh();

                textureImporter.isReadable = originalValue;
                textureImporter.SaveAndReimport();
            }
        }
    }
}
