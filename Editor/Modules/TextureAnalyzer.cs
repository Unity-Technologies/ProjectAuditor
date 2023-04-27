using System.Collections.Generic;
using System.IO;
using Unity.ProjectAuditor.Editor.Core;
using Unity.ProjectAuditor.Editor.Diagnostic;
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
        internal const string PAA0007 = nameof(PAA0007);

        internal static readonly Descriptor k_TextureMipMapNotEnabledDescriptor = new Descriptor(
            PAA0000,
            "Texture: Mipmaps not enabled",
            new[] {Area.GPU, Area.Quality},
            "Texture mipmaps generation is not enabled. Generally enabling mipmaps improves rendering quality (avoids aliasing effects) and improves performance.",
            "Consider enabling mipmaps using the <b>Advanced ➔ Generate Mip Maps</b> option in the texture inspector."
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
            "Texture mipmaps generation is enabled. This might reduce rendering quality of sprites and UI.",
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
            "Mesh Read/Write flag is enabled. This causes the texture data to be duplicated in memory.",
            "If not required, consider disabling the <b>Read/Write Enabled</b> option in the texture inspector."
        )
        {
            messageFormat = "Texture '{0}' Read/Write is enabled",
            documentationUrl = "https://docs.unity3d.com/ScriptReference/Texture-isReadable.html",
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
            "Texture mipmaps streaming is not enabled. This increases the amount of mipmap textures that are loaded into memory on the GPU.",
            "Consider enabled mipmaps streaming using the <b>Streaming Mipmaps</b> option in the texture inspector."
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
            "The anisotropic level is higher than 1. Anisotropic filtering makes textures look better when viewed at a shallow angle, but it can be slower to process on the GPU.",
            "Consider setting the anisotropic level to 1."
        )
        {
            platforms = new[] {"Android", "iOS", "Switch"},
            messageFormat = "Texture '{0}' has an anisotropic level higher than 1.",
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
            "The texture is a solid color. This increases the amount of memory usage and can be reduced.",
            "Consider shrinking the texture to 1x1 size."
        )
        {
            messageFormat = "Texture '{0}' is a solid color and not 1x1 size",
            fixer = (issue) => { ResizeSolidTexture(issue.relativePath); }
        };

        internal static readonly Descriptor k_AtlasTextureEmptyDescriptor = new Descriptor(
            PAA0007,
            "Atlas Texture : Too much empty space",
            new[] {Area.Memory},
            "The Atlas Texture texture has too much empty space. This increases the amount of memory usage and can be reduced.",
            "Consider reorganizing your Atlas Texture."
        )
        {
            messageFormat = "Atlas Texture '{0}' has too much empty space ({1} %)."
        };

        public void Initialize(ProjectAuditorModule module)
        {
            module.RegisterDescriptor(k_TextureMipMapNotEnabledDescriptor);
            module.RegisterDescriptor(k_TextureMipMapEnabledDescriptor);
            module.RegisterDescriptor(k_TextureReadWriteEnabledDescriptor);
            module.RegisterDescriptor(k_TextureStreamingMipMapEnabledDescriptor);
            module.RegisterDescriptor(k_TextureAnisotropicLevelDescriptor);
            module.RegisterDescriptor(k_TextureSolidColorDescriptor);
            module.RegisterDescriptor(k_AtlasTextureEmptyDescriptor);
        }

        public IEnumerable<ProjectIssue> Analyze(ProjectAuditorParams projectAuditorParams, TextureImporter textureImporter, TextureImporterPlatformSettings platformSettings)
        {
            var assetPath = textureImporter.assetPath;

            // TODO: the size returned by the profiler is not the exact size on the target platform. Needs to be fixed.
            var texture = AssetDatabase.LoadAssetAtPath<Texture>(assetPath);
            var size = Profiler.GetRuntimeMemorySizeLong(texture);
            var resolution = texture.width + "x" + texture.height;

            yield return ProjectIssue.Create(IssueCategory.Texture, texture.name)
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
                    k_TextureMipMapNotEnabledDescriptor, textureName)
                    .WithLocation(assetPath);
            }

            if (textureImporter.mipmapEnabled &&
                (textureImporter.textureType == TextureImporterType.Sprite || textureImporter.textureType == TextureImporterType.GUI)
            )
            {
                yield return ProjectIssue.Create(IssueCategory.AssetDiagnostic,
                    k_TextureMipMapEnabledDescriptor, textureName)
                    .WithLocation(assetPath);
            }

            if (textureImporter.isReadable)
            {
                yield return ProjectIssue.Create(IssueCategory.AssetDiagnostic, k_TextureReadWriteEnabledDescriptor, textureName)
                    .WithLocation(textureImporter.assetPath);
            }

            if (textureImporter.mipmapEnabled && !textureImporter.streamingMipmaps && size > Mathf.Pow(projectAuditorParams.settings.TextureStreamingMipmapsSizeLimit, 2))
            {
                yield return ProjectIssue.Create(IssueCategory.AssetDiagnostic, k_TextureStreamingMipMapEnabledDescriptor, textureName)
                    .WithLocation(textureImporter.assetPath);
            }

            if (textureImporter.mipmapEnabled && textureImporter.filterMode != FilterMode.Point && textureImporter.anisoLevel > 1)
            {
                yield return ProjectIssue.Create(IssueCategory.AssetDiagnostic, k_TextureAnisotropicLevelDescriptor, textureName)
                    .WithLocation(textureImporter.assetPath);
            }

            if (TextureUtils.IsTextureSolidColorTooBig(textureImporter, texture))
            {
                yield return ProjectIssue.Create(IssueCategory.AssetDiagnostic, k_TextureSolidColorDescriptor, textureName)
                    .WithLocation(textureImporter.assetPath);
            }

            var texture2D = texture as Texture2D;
            if (texture2D != null)
            {
                var emptyPercent = TextureUtils.GetEmptyPixelsPercent(texture2D);
                if (emptyPercent >
                    projectAuditorParams.settings.SpriteAtlasEmptySpaceLimit)
                {
                    yield return ProjectIssue.Create(IssueCategory.AssetDiagnostic, k_AtlasTextureEmptyDescriptor, textureName, emptyPercent)
                        .WithLocation(textureImporter.assetPath);
                }
            }
            else
            {
                Debug.LogError(texture.name + " is not a Texture2D!");
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
