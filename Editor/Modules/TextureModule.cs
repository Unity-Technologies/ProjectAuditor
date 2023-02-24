using System;
using System.Collections.Generic;
using System.Linq;
using Unity.ProjectAuditor.Editor.Core;
using Unity.ProjectAuditor.Editor.Diagnostic;
using UnityEditor;

namespace Unity.ProjectAuditor.Editor.Modules
{
    public enum TextureProperty
    {
        Shape,
        ImporterType,
        Format,
        TextureCompression,
        MipMapEnabled,
        Readable,
        Resolution,
        SizeOnDisk,
        StreamingMipMap,
        Num
    }

    class TextureModule : ProjectAuditorModuleWithAnalyzers<ITextureModuleAnalyzer>
    {
        static readonly IssueLayout k_TextureLayout = new IssueLayout
        {
            category = IssueCategory.Texture,
            properties = new[]
            {
                new PropertyDefinition { type = PropertyType.Description, format = PropertyFormat.String, name = "Name", longName = "Texture Name" },
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(TextureProperty.Shape), format = PropertyFormat.String, name = "Shape", longName = "Texture Shape" },
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(TextureProperty.ImporterType), format = PropertyFormat.String, name = "Importer Type", longName = "Texture Importer Type" },
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(TextureProperty.Format), format = PropertyFormat.String, name = "Format", longName = "Texture Format" },
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(TextureProperty.TextureCompression), format = PropertyFormat.String, name = "Compression", longName = "Texture Compression" },
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(TextureProperty.MipMapEnabled), format = PropertyFormat.Bool, name = "MipMaps", longName = "Texture MipMaps Enabled" },
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(TextureProperty.Readable), format = PropertyFormat.Bool, name = "Readable", longName = "Readable" },
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(TextureProperty.Resolution), format = PropertyFormat.String, name = "Resolution", longName = "Texture Resolution" },
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(TextureProperty.SizeOnDisk), format = PropertyFormat.Bytes, name = "Size", longName = "Texture Size" },
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(TextureProperty.StreamingMipMap), format = PropertyFormat.Bool, name = "Streaming", longName = "Mipmaps Streaming" },
                new PropertyDefinition { type = PropertyType.Path, name = "Path"}
            }
        };

        public override string name => "Textures";

        public override bool isEnabledByDefault => false;

        public override IReadOnlyCollection<IssueLayout> supportedLayouts => new IssueLayout[]
        {
            k_TextureLayout,
            AssetsModule.k_IssueLayout
        };

        public override void Audit(ProjectAuditorParams projectAuditorParams, IProgress progress = null)
        {
            var analyzers = GetPlatformAnalyzers(projectAuditorParams.platform);
            var allTextures = AssetDatabase.FindAssets("t:texture, a:assets");
            var currentPlatformString = projectAuditorParams.platform.ToString();

            progress?.Start("Finding Textures", "Search in Progress...", allTextures.Length);

            foreach (var guid in allTextures)
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(guid);
                var textureImporter = AssetImporter.GetAtPath(assetPath) as TextureImporter;
                if (textureImporter == null)
                {
                    continue; // skip render textures
                }

                var platformSettings = textureImporter.GetPlatformTextureSettings(currentPlatformString);
                foreach (var analyzer in analyzers)
                {
                    projectAuditorParams.onIncomingIssues(analyzer.Analyze(projectAuditorParams, textureImporter, platformSettings));
                }

                progress?.Advance();
            }

            progress?.Clear();

            projectAuditorParams.onModuleCompleted?.Invoke();
        }
    }
}
