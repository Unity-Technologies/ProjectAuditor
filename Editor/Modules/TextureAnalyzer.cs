using System.Collections.Generic;
using System.IO;
using Unity.ProjectAuditor.Editor.Core;
using Unity.ProjectAuditor.Editor.Diagnostic;
using Unity.ProjectAuditor.Editor.Modules;
using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;

namespace Unity.ProjectAuditor.Editor.Modules
{
    public class TextureAnalyzer : ITextureModuleAnalyzer
    {
        internal const string PAT0000 = nameof(PAT0000);
        internal const string PAT0001 = nameof(PAT0001);
        internal const string PAT0002 = nameof(PAT0002);
        internal const string PAT0003 = nameof(PAT0003);

        internal static readonly Descriptor k_TextureMipMapNotEnabledDescriptor = new Descriptor(
            PAT0000,
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
            PAT0001,
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
            PAT0002,
            "Texture: Read/Write enabled",
            Area.Memory,
            "Mesh Read/Write flag is enabled. This causes the texture data to be duplicated in memory." +
            "Thus, this option should only be used if the texture is read or written to at run-time." +
            "Consider disabling Read/Write using the <b>Read/Write Enabled</b> option in the texture inspector."
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
            PAT0003,
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

        public void Initialize(ProjectAuditorModule module)
        {
            module.RegisterDescriptor(k_TextureMipMapNotEnabledDescriptor);
            module.RegisterDescriptor(k_TextureMipMapEnabledDescriptor);
            module.RegisterDescriptor(k_TextureReadWriteEnabledDescriptor);
            module.RegisterDescriptor(k_TextureStreamingMipMapEnabledDescriptor);
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

            if (!textureImporter.streamingMipmaps && size > Mathf.Pow(projectAuditorParams.settings.TextureStreamingMipmapsSizeLimit, 2))
            {
                yield return ProjectIssue.Create(IssueCategory.AssetDiagnostic, k_TextureStreamingMipMapEnabledDescriptor, textureName)
                    .WithLocation(textureImporter.assetPath);
            }
        }
    }
}
