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
        internal static readonly Descriptor k_TextureMipMapNotEnabledDescriptor = new Descriptor(
            "PAT0000",
            "Texture: Mip Maps not enabled",
            new[] {Area.GPU, Area.Quality},
            "Texture's Mip Maps are not enabled.\n\nGenerally enabling mip maps improves rendering quality (avoids aliasing effects) and improves performance.",
            "Select the texture asset and, if applicable, enable texture importer option <b>Advanced / Generate Mip Maps</b>."
        )
        {
            messageFormat = "Texture '{0}' mip maps are not enabled",
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
            "PAT0001",
            "Texture: Mip Maps enabled on 2D texture",
            new[] {Area.BuildSize, Area.Quality},
            "Texture's Mip Maps are enabled on textures that may reduce rendering quality for Sprites or GUI. Disabling Mip Maps also reduces your build size.\n\nPlease verify if this is relevant for this texture.",
            "Select the texture asset and, if applicable, disable texture importer option <b>Advanced / Generate Mip Maps</b>. This will also reduce your build size."
        )
        {
            messageFormat = "Texture '{0}' mip maps are enabled",
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
            "PAT0002",
            "Texture: Read/Write enabled",
            Area.Memory,
            "Mesh's Read/Write flag is enabled. This causes the texture data to be duplicated in memory." +
            "\n\nEnabling Read/Write access may be needed if this mesh is read or written to at run-time." +
            "\n\nRefer to the 'Texture.isReadable' documentation for more details on cases when this should stay enabled.",
            "Select the asset and disable import settings <b>Read/Write Enabled</b> option, then click the <b>Apply</b> button."
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

        public void Initialize(ProjectAuditorModule module)
        {
            module.RegisterDescriptor(k_TextureMipMapNotEnabledDescriptor);
            module.RegisterDescriptor(k_TextureMipMapEnabledDescriptor);
            module.RegisterDescriptor(k_TextureReadWriteEnabledDescriptor);
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
                        size
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
        }
    }
}
