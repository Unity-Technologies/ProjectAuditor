using System.Collections.Generic;
using System.IO;
using Unity.ProjectAuditor.Editor.Core;
using Unity.ProjectAuditor.Editor.Diagnostic;
using Unity.ProjectAuditor.Editor.Modules;
using UnityEditor;

namespace Unity.ProjectAuditor.Editor
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
            messageFormat = "Texture '{0}' mip maps are not enabled"
        };

        internal static readonly Descriptor k_TextureMipMapEnabledDescriptor = new Descriptor(
            "PAT0001",
            "Texture: Mip Maps enabled on 2D texture",
            new[] {Area.Quality},
            "Texture's Mip Maps are enabled on textures that may reduce rendering quality for Sprites or GUI. Disabling Mip Maps also reduces your build size.\n\nPlease verify if this is relevant for this texture.",
            "Select the texture asset and, if applicable, disable texture importer option <b>Advanced / Generate Mip Maps</b>."
        )
        {
            messageFormat = "Texture '{0}' mip maps are enabled"
        };

        public void Initialize(ProjectAuditorModule module)
        {
            module.RegisterDescriptor(k_TextureMipMapNotEnabledDescriptor);
            module.RegisterDescriptor(k_TextureMipMapEnabledDescriptor);
        }

        public IEnumerable<ProjectIssue> Analyze(BuildTarget platform, TextureImporter textureImporter, TextureImporterPlatformSettings textureImporterPlatformSettings)
        {
            if (textureImporter.mipmapEnabled == false && textureImporter.textureType == TextureImporterType.Default)
            {
                var assetPath = textureImporter.assetPath;
                var textureName = Path.GetFileNameWithoutExtension(assetPath);

                yield return ProjectIssue.Create(IssueCategory.TextureDiagnostic,
                    k_TextureMipMapNotEnabledDescriptor, textureName)
                    .WithLocation(assetPath);
            }

            if (textureImporter.mipmapEnabled == true &&
                (textureImporter.textureType == TextureImporterType.Sprite || textureImporter.textureType == TextureImporterType.GUI)
            )
            {
                var assetPath = textureImporter.assetPath;
                var textureName = Path.GetFileNameWithoutExtension(assetPath);

                yield return ProjectIssue.Create(IssueCategory.TextureDiagnostic,
                    k_TextureMipMapEnabledDescriptor, textureName)
                    .WithLocation(assetPath);
            }
        }
    }
}
