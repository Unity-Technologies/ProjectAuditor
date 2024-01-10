using System.Collections.Generic;
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

        internal static readonly Descriptor k_TextureMipMapNotEnabledDescriptor = new Descriptor(
            PAA0000,
            "Texture: Mipmaps not enabled",
            Areas.GPU | Areas.Quality,
            "<b>Generate Mip Maps</b> in the Texture Import Settings is not enabled. Using textures that are not mipmapped in a 3D environment can impact rendering performance and introduce aliasing artifacts.",
            "Consider enabling mipmaps using the <b>Advanced > Generate Mip Maps</b> option in the Texture Import Settings."
        )
        {
            MessageFormat = "Texture2D '{0}' mipmaps generation is not enabled",
            Fixer = (issue, analysisParams) =>
            {
                var textureImporter = AssetImporter.GetAtPath(issue.RelativePath) as TextureImporter;
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
            Areas.BuildSize | Areas.Quality,
            "<b>Generate Mip Maps</b> is enabled in the Texture Import Settings for a Sprite/UI texture. This might reduce rendering quality of sprites and UI.",
            "Consider disabling mipmaps using the <b>Advanced > Generate Mip Maps</b> option in the texture inspector. This will also reduce your build size."
        )
        {
            MessageFormat = "Texture2D '{0}' mipmaps generation is enabled",
            Fixer = (issue, analysisParams) =>
            {
                var textureImporter = AssetImporter.GetAtPath(issue.RelativePath) as TextureImporter;
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
            Areas.Memory,
            "The <b>Read/Write Enabled</b> flag in the Texture Import Settings is enabled. This causes the texture data to be duplicated in memory.",
            "If not required, disable the <b>Read/Write Enabled</b> option in the Texture Import Settings."
        )
        {
            MessageFormat = "Texture2D '{0}' Read/Write is enabled",
            DocumentationUrl = "https://docs.unity3d.com/Manual/class-TextureImporter.html",
            Fixer = (issue, analysisParams) =>
            {
                var textureImporter = AssetImporter.GetAtPath(issue.RelativePath) as TextureImporter;
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
            Areas.Memory | Areas.Quality,
            "The <b>Streaming Mipmaps</b> option in the Texture Import Settings is not enabled. As a result, all mip levels for this texture are loaded into GPU memory for as long as the texture is loaded, potentially resulting in excessive texture memory usage.",
            "Consider enabling the <b>Streaming Mipmaps</b> option in the Texture Import Settings."
        )
        {
            MessageFormat = "Texture2D '{0}' mipmaps streaming is not enabled",
            Fixer = (issue, analysisParams) =>
            {
                var textureImporter = AssetImporter.GetAtPath(issue.RelativePath) as TextureImporter;
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
            Areas.GPU | Areas.Quality,
            "The <b>Anisotropic Level</b> in the Texture Import Settings is higher than 1. Anisotropic filtering makes textures look better when viewed at a shallow angle, but it can be slower to process on the GPU.",
            "Consider setting the <b>Anisotropic Level</b> to 1."
        )
        {
            Platforms = new[] { BuildTarget.Android, BuildTarget.iOS, BuildTarget.Switch},
            MessageFormat = "Texture2D '{0}' anisotropic level is set to '{1}'",
            Fixer = (issue, analysisParams) =>
            {
                var textureImporter = AssetImporter.GetAtPath(issue.RelativePath) as TextureImporter;
                if (textureImporter != null)
                {
                    textureImporter.anisoLevel = 1;
                    textureImporter.SaveAndReimport();
                }
            }
        };

        public void Initialize(Module module)
        {
            module.RegisterDescriptor(k_TextureMipMapNotEnabledDescriptor);
            module.RegisterDescriptor(k_TextureMipMapEnabledDescriptor);
            module.RegisterDescriptor(k_TextureReadWriteEnabledDescriptor);
            module.RegisterDescriptor(k_TextureStreamingMipMapEnabledDescriptor);
            module.RegisterDescriptor(k_TextureAnisotropicLevelDescriptor);
        }

        public IEnumerable<ProjectIssue> Analyze(TextureAnalysisContext context)
        {
            var assetPath = context.Importer.assetPath;

#if PA_CAN_USE_COMPUTEMIPCHAINSIZE
            var format = (TextureFormat)context.ImporterPlatformSettings.format;
            if (context.ImporterPlatformSettings.format == TextureImporterFormat.Automatic)
            {
                format = (TextureFormat)context.Importer.GetAutomaticFormat(context.Params.PlatformAsString);
            }

            var size = UnityEngine.Experimental.Rendering.GraphicsFormatUtility.ComputeMipChainSize(context.Texture.width, context.Texture.height, TextureUtils.GetTextureDepth(context.Texture), format, context.Texture.mipmapCount);
#else
            // This is not the correct size but we don't have access to the appropriate functionality on older versions to do much better without a lot more work.
            var size = Profiler.GetRuntimeMemorySizeLong(context.Texture);
#endif
            var resolution = context.Texture.width + "x" + context.Texture.height;

            yield return context.CreateInsight(IssueCategory.Texture, context.Texture.name)
                .WithCustomProperties(
                    new object[(int)TextureProperty.Num]
                    {
                        context.Importer.textureShape,
                        context.Importer.textureType,
                        context.ImporterPlatformSettings.format,
                        context.ImporterPlatformSettings.textureCompression,
                        context.Importer.mipmapEnabled,
                        context.Importer.isReadable,
                        resolution,
                        size,
                        context.Importer.streamingMipmaps
                    })
                .WithLocation(new Location(assetPath));

            // diagnostics
            if (!context.Importer.mipmapEnabled && context.Importer.textureType == TextureImporterType.Default)
            {
                yield return context.CreateIssue(IssueCategory.AssetDiagnostic,
                    k_TextureMipMapNotEnabledDescriptor.Id, context.Name)
                    .WithLocation(assetPath);
            }

            if (context.Importer.mipmapEnabled &&
                (context.Importer.textureType == TextureImporterType.Sprite || context.Importer.textureType == TextureImporterType.GUI)
            )
            {
                yield return context.CreateIssue(IssueCategory.AssetDiagnostic,
                    k_TextureMipMapEnabledDescriptor.Id, context.Name)
                    .WithLocation(assetPath);
            }

            if (context.Importer.isReadable)
            {
                yield return context.CreateIssue(IssueCategory.AssetDiagnostic, k_TextureReadWriteEnabledDescriptor.Id, context.Name)
                    .WithLocation(context.Importer.assetPath);
            }

            if (context.Importer.mipmapEnabled && !context.Importer.streamingMipmaps && size > Mathf.Pow(context.TextureStreamingMipmapsSizeLimit, 2))
            {
                yield return context.CreateIssue(IssueCategory.AssetDiagnostic, k_TextureStreamingMipMapEnabledDescriptor.Id, context.Name)
                    .WithLocation(context.Importer.assetPath);
            }

            if (k_TextureAnisotropicLevelDescriptor.IsApplicable(context.Params) &&
                context.Importer.mipmapEnabled && context.Importer.filterMode != FilterMode.Point && context.Importer.anisoLevel > 1)
            {
                yield return context.CreateIssue(IssueCategory.AssetDiagnostic, k_TextureAnisotropicLevelDescriptor.Id, context.Name, context.Importer.anisoLevel)
                    .WithLocation(context.Importer.assetPath);
            }
        }
    }
}
