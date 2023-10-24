using System;
using System.Collections.Generic;
using System.IO;
using Unity.ProjectAuditor.Editor.Core;
using Unity.ProjectAuditor.Editor.Interfaces;
using UnityEditor;
using UnityEngine;

namespace Unity.ProjectAuditor.Editor.Modules
{
    internal enum TextureProperty
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
            var analyzers = GetPlatformAnalyzers(projectAuditorParams.Platform);
            foreach (var analyzer in analyzers)
            {
                analyzer.PrepareForAnalysis(projectAuditorParams);
            }

            var assetPaths = GetAssetPathsByFilter("t:texture, a:assets");
            var currentPlatformString = projectAuditorParams.PlatformString;

            progress?.Start("Finding Textures", "Search in Progress...", assetPaths.Length);

            foreach (var assetPath in assetPaths)
            {
                var textureImporter = AssetImporter.GetAtPath(assetPath) as TextureImporter;
                if (textureImporter == null)
                {
                    continue; // skip render textures
                }

                var context = new TextureAnalysisContext
                {
                    Importer = textureImporter,
                    ImporterPlatformSettings = textureImporter.GetPlatformTextureSettings(currentPlatformString),
                    Texture = AssetDatabase.LoadAssetAtPath<Texture>(assetPath),
                    Params = projectAuditorParams
                };

                if (string.IsNullOrEmpty(context.Texture.name))
                    context.Name = Path.GetFileNameWithoutExtension(assetPath);
                else
                    context.Name = context.Texture.name;

                foreach (var analyzer in analyzers)
                {
                    projectAuditorParams.OnIncomingIssues(analyzer.Analyze(context));
                }

                progress?.Advance();
            }

            progress?.Clear();

            projectAuditorParams.OnModuleCompleted?.Invoke();
        }
    }
}
