using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using Unity.ProjectAuditor.Editor.Core;
using Unity.ProjectAuditor.Editor.Diagnostic;
using Unity.ProjectAuditor.Editor.Interfaces;
using Unity.ProjectAuditor.Editor.Utils;
using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;

namespace Unity.ProjectAuditor.Editor.Modules
{
    class AudioClipAnalyzer : IAudioClipModuleAnalyzer
    {
        // internal const string PAA0000 = nameof(PAA0000);
        // internal const string PAA0001 = nameof(PAA0001);
        // internal const string PAA0002 = nameof(PAA0002);
        // internal const string PAA0003 = nameof(PAA0003);
        // internal const string PAA0004 = nameof(PAA0004);
        // internal const string PAA0005 = nameof(PAA0005);
        // internal const string PAA0006 = nameof(PAA0006);
        // internal const string PAA0007 = nameof(PAA0007);
        //
        // internal static readonly Descriptor k_TextureMipMapNotEnabledDescriptor = new Descriptor(
        //     PAA0000,
        //     "Texture: Mipmaps not enabled",
        //     new[] {Area.GPU, Area.Quality},
        //     "<b>Generate Mip Maps</b> in the Texture Import Settings is not enabled. Using textures that are not mipmapped in a 3D environment can impact rendering performance and introduce aliasing artifacts.",
        //     "Consider enabling mipmaps using the <b>Advanced ➔ Generate Mip Maps</b> option in the Texture Import Settings."
        // )
        // {
        //     messageFormat = "Texture '{0}' mipmaps generation is not enabled",
        //     fixer = (issue) =>
        //     {
        //         var textureImporter = AssetImporter.GetAtPath(issue.relativePath) as TextureImporter;
        //         if (textureImporter != null)
        //         {
        //             textureImporter.mipmapEnabled = true;
        //             textureImporter.SaveAndReimport();
        //         }
        //     }
        // };
        //
        // internal static readonly Descriptor k_TextureMipMapEnabledDescriptor = new Descriptor(
        //     PAA0001,
        //     "Texture: Mipmaps enabled on Sprite/UI texture",
        //     new[] {Area.BuildSize, Area.Quality},
        //     "<b>Generate Mip Maps</b> is enabled in the Texture Import Settings for a Sprite/UI texture. This might reduce rendering quality of sprites and UI.",
        //     "Consider disabling mipmaps using the <b>Advanced ➔ Generate Mip Maps</b> option in the texture inspector. This will also reduce your build size."
        // )
        // {
        //     messageFormat = "Texture '{0}' mipmaps generation is enabled",
        //     fixer = (issue) =>
        //     {
        //         var textureImporter = AssetImporter.GetAtPath(issue.relativePath) as TextureImporter;
        //         if (textureImporter != null)
        //         {
        //             textureImporter.mipmapEnabled = false;
        //             textureImporter.SaveAndReimport();
        //         }
        //     }
        // };
        //
        // internal static readonly Descriptor k_TextureReadWriteEnabledDescriptor = new Descriptor(
        //     PAA0002,
        //     "Texture: Read/Write enabled",
        //     Area.Memory,
        //     "The <b>Read/Write Enabled</b> flag in the Texture Import Settings is enabled. This causes the texture data to be duplicated in memory.",
        //     "If not required, disable the <b>Read/Write Enabled</b> option in the Texture Import Settings."
        // )
        // {
        //     messageFormat = "Texture '{0}' Read/Write is enabled",
        //     documentationUrl = "https://docs.unity3d.com/Manual/class-TextureImporter.html",
        //     fixer = (issue) =>
        //     {
        //         var textureImporter = AssetImporter.GetAtPath(issue.relativePath) as TextureImporter;
        //         if (textureImporter != null)
        //         {
        //             textureImporter.isReadable = false;
        //             textureImporter.SaveAndReimport();
        //         }
        //     }
        // };
        //
        // internal static readonly Descriptor k_TextureStreamingMipMapEnabledDescriptor = new Descriptor(
        //     PAA0003,
        //     "Texture: Mipmaps Streaming not enabled",
        //     new[] {Area.Memory, Area.Quality},
        //     "The <b>Streaming Mipmaps</b> option in the Texture Import Settings is not enabled. As a result, all mip levels for this texture are loaded into GPU memory for as long as the texture is loaded, potentially resulting in excessive texture memory usage.",
        //     "Consider enabling the <b>Streaming Mipmaps</b> option in the Texture Import Settings."
        // )
        // {
        //     messageFormat = "Texture '{0}' mipmaps streaming is not enabled",
        //     fixer = (issue) =>
        //     {
        //         var textureImporter = AssetImporter.GetAtPath(issue.relativePath) as TextureImporter;
        //         if (textureImporter != null)
        //         {
        //             textureImporter.streamingMipmaps = true;
        //             textureImporter.SaveAndReimport();
        //         }
        //     }
        // };
        //
        // internal static readonly Descriptor k_TextureAnisotropicLevelDescriptor = new Descriptor(
        //     PAA0004,
        //     "Texture: Anisotropic level is higher than 1",
        //     new[] {Area.GPU, Area.Quality},
        //     "The <b>Anisotropic Level</b> in the Texture Import Settings is higher than 1. Anisotropic filtering makes textures look better when viewed at a shallow angle, but it can be slower to process on the GPU.",
        //     "Consider setting the <b>Anisotropic Level</b> to 1."
        // )
        // {
        //     platforms = new[] {"Android", "iOS", "Switch"},
        //     messageFormat = "Texture '{0}' anisotropic level is set to '{1}'",
        //     fixer = (issue) =>
        //     {
        //         var textureImporter = AssetImporter.GetAtPath(issue.relativePath) as TextureImporter;
        //         if (textureImporter != null)
        //         {
        //             textureImporter.anisoLevel = 1;
        //             textureImporter.SaveAndReimport();
        //         }
        //     }
        // };
        //
        // internal static readonly Descriptor k_TextureSolidColorDescriptor = new Descriptor(
        //     PAA0005,
        //     "Texture: Solid color is not 1x1 size",
        //     new[] {Area.Memory},
        //     "The texture is a single, solid color and is bigger than 1x1 pixels in size. Redundant texture data occupies memory unneccesarily.",
        //     "Consider shrinking the texture to 1x1 size."
        // )
        // {
        //     messageFormat = "Texture '{0}' is a solid color and not 1x1 size",
        //     fixer = (issue) => { ResizeSolidTexture(issue.relativePath); }
        // };
        //
        // // NOTE:  This is only here to run the same analysis without a quick fix button.  Clean up when we either have appropriate quick fix for other dimensions or improved fixer support.
        // internal static readonly Descriptor k_TextureSolidColorNoFixerDescriptor = new Descriptor(
        //     PAA0006,
        //     "Texture: Solid color is not 1x1 size",
        //     new[] { Area.Memory },
        //     "The texture is a single, solid color and is bigger than 1x1 pixels in size. Redundant texture data occupies memory unneccesarily.",
        //     "Consider shrinking the texture to 1x1 size."
        // )
        // {
        //     messageFormat = "Texture '{0}' is a solid color and not 1x1 size"
        // };
        //
        // internal static readonly Descriptor k_TextureAtlasEmptyDescriptor = new Descriptor(
        //     PAA0007,
        //     "Texture Atlas: Too much empty space",
        //     new[] {Area.Memory},
        //     "The texture atlas contains a lot of empty space. Empty space contributes to texture memory usage.",
        //     "Consider reorganizing your texture atlas in order to reduce the amount of empty space."
        // )
        // {
        //     messageFormat = "Texture Atlas '{0}' has too much empty space ({1} %)"
        // };

        public void Initialize(ProjectAuditorModule module)
        {
            // module.RegisterDescriptor(k_TextureMipMapNotEnabledDescriptor);
            // module.RegisterDescriptor(k_TextureMipMapEnabledDescriptor);
            // module.RegisterDescriptor(k_TextureReadWriteEnabledDescriptor);
            // module.RegisterDescriptor(k_TextureStreamingMipMapEnabledDescriptor);
            // module.RegisterDescriptor(k_TextureAnisotropicLevelDescriptor);
            // module.RegisterDescriptor(k_TextureSolidColorDescriptor);
            // module.RegisterDescriptor(k_TextureSolidColorNoFixerDescriptor);
            // module.RegisterDescriptor(k_TextureAtlasEmptyDescriptor);
        }

        public IEnumerable<ProjectIssue> Analyze(ProjectAuditorParams projectAuditorParams, AudioImporter audioImporter)
        {
            var assetPath = audioImporter.assetPath;
            var sampleSettings = audioImporter.GetOverrideSampleSettings(projectAuditorParams.platform.ToString());
            var audioClip = AssetDatabase.LoadAssetAtPath<AudioClip>(assetPath);

            // TODO: the size returned by the profiler is not the exact size on the target platform. Needs to be fixed.
            var runtimeSize = Profiler.GetRuntimeMemorySizeLong(audioClip);
            var origSize = (int)GetPropertyValue(audioImporter, "origSize");
            var compSize = (int)GetPropertyValue(audioImporter, "compSize");

            var ts = new TimeSpan(0, 0, 0, 0, (int)(audioClip.length * 1000.0f));

            yield return ProjectIssue.Create(IssueCategory.AudioClip, Path.GetFileNameWithoutExtension(assetPath))
                .WithCustomProperties(
                    new object[(int)AudioClipProperty.Num]
            {
                String.Format("{0:00}:{1:00}.{2:000}", ts.Minutes, ts.Seconds, ts.Milliseconds),
                origSize,
                compSize,
                runtimeSize,
                (100.0f * (float)compSize / (float)origSize).ToString("0.00", CultureInfo.InvariantCulture.NumberFormat) + "%",
                sampleSettings.compressionFormat,
                ((float)audioClip.frequency / 1000.0f).ToString("G0", CultureInfo.InvariantCulture.NumberFormat) + " KHz",
                audioImporter.forceToMono,
                audioImporter.loadInBackground,
#if UNITY_2022_2_OR_NEWER
                sampleSettings.preloadAudioData,
#else
                audioImporter.preloadAudioData,
#endif
                sampleSettings.loadType,

            }).WithLocation(assetPath);


//             // diagnostics
//             var textureName = Path.GetFileNameWithoutExtension(assetPath);
//
//             if (!textureImporter.mipmapEnabled && textureImporter.textureType == TextureImporterType.Default)
//             {
//                 yield return ProjectIssue.Create(IssueCategory.AssetDiagnostic,
//                     k_TextureMipMapNotEnabledDescriptor, textureName)
//                     .WithLocation(assetPath);
//             }
//
//             if (textureImporter.mipmapEnabled &&
//                 (textureImporter.textureType == TextureImporterType.Sprite || textureImporter.textureType == TextureImporterType.GUI)
//             )
//             {
//                 yield return ProjectIssue.Create(IssueCategory.AssetDiagnostic,
//                     k_TextureMipMapEnabledDescriptor, textureName)
//                     .WithLocation(assetPath);
//             }
//
//             if (textureImporter.isReadable)
//             {
//                 yield return ProjectIssue.Create(IssueCategory.AssetDiagnostic, k_TextureReadWriteEnabledDescriptor, textureName)
//                     .WithLocation(textureImporter.assetPath);
//             }
//
//             if (textureImporter.mipmapEnabled && !textureImporter.streamingMipmaps && size > Mathf.Pow(projectAuditorParams.settings.TextureStreamingMipmapsSizeLimit, 2))
//             {
//                 yield return ProjectIssue.Create(IssueCategory.AssetDiagnostic, k_TextureStreamingMipMapEnabledDescriptor, textureName)
//                     .WithLocation(textureImporter.assetPath);
//             }
//
//             if (textureImporter.mipmapEnabled && textureImporter.filterMode != FilterMode.Point && textureImporter.anisoLevel > 1)
//             {
//                 yield return ProjectIssue.Create(IssueCategory.AssetDiagnostic, k_TextureAnisotropicLevelDescriptor, textureName, textureImporter.anisoLevel)
//                     .WithLocation(textureImporter.assetPath);
//             }
//
//             if (TextureUtils.IsTextureSolidColorTooBig(textureImporter, texture))
//             {
//                 var dimensionAppropriateDescriptor = texture.dimension == UnityEngine.Rendering.TextureDimension.Tex2D ? k_TextureSolidColorDescriptor : k_TextureSolidColorNoFixerDescriptor;
//                 yield return ProjectIssue.Create(IssueCategory.AssetDiagnostic, dimensionAppropriateDescriptor, textureName)
//                     .WithLocation(textureImporter.assetPath);
//             }
//
//             var texture2D = texture as Texture2D;
//             if (texture2D != null)
//             {
//                 var emptyPercent = TextureUtils.GetEmptyPixelsPercent(texture2D);
//                 if (emptyPercent >
//                     projectAuditorParams.settings.SpriteAtlasEmptySpaceLimit)
//                 {
//                     yield return ProjectIssue.Create(IssueCategory.AssetDiagnostic, k_TextureAtlasEmptyDescriptor, textureName, emptyPercent)
//                         .WithLocation(textureImporter.assetPath);
//                 }
//             }
        }

        private static object GetPropertyValue(AssetImporter assetImporter, string propertyName)
        {
            Type objType = assetImporter.GetType();
            PropertyInfo propInfo = objType.GetProperty(propertyName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            if (propInfo == null)
                throw new ArgumentOutOfRangeException("propertyName",
                    string.Format("Couldn't find property {0} in type {1}", propertyName, objType.FullName));
            return propInfo.GetValue(assetImporter, null);
        }
    }
}
