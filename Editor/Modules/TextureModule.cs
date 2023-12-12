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

    class TextureModule : ModuleWithAnalyzers<ITextureModuleAnalyzer>
    {
        static readonly IssueLayout k_TextureLayout = new IssueLayout
        {
            Category = IssueCategory.Texture,
            Properties = new[]
            {
                new PropertyDefinition { Type = PropertyType.Description, Format = PropertyFormat.String, Name = "Name", LongName = "Texture Name", MaxAutoWidth = 500 },
                new PropertyDefinition { Type = PropertyTypeUtil.FromCustom(TextureProperty.Shape), Format = PropertyFormat.String, Name = "Shape", LongName = "Texture Shape" },
                new PropertyDefinition { Type = PropertyTypeUtil.FromCustom(TextureProperty.ImporterType), Format = PropertyFormat.String, Name = "Importer Type", LongName = "Texture Importer Type" },
                new PropertyDefinition { Type = PropertyTypeUtil.FromCustom(TextureProperty.Format), Format = PropertyFormat.String, Name = "Format", LongName = "Texture Format" },
                new PropertyDefinition { Type = PropertyTypeUtil.FromCustom(TextureProperty.TextureCompression), Format = PropertyFormat.String, Name = "Compression", LongName = "Texture Compression" },
                new PropertyDefinition { Type = PropertyTypeUtil.FromCustom(TextureProperty.MipMapEnabled), Format = PropertyFormat.Bool, Name = "MipMaps", LongName = "Texture MipMaps Enabled" },
                new PropertyDefinition { Type = PropertyTypeUtil.FromCustom(TextureProperty.Readable), Format = PropertyFormat.Bool, Name = "Readable", LongName = "Readable" },
                new PropertyDefinition { Type = PropertyTypeUtil.FromCustom(TextureProperty.Resolution), Format = PropertyFormat.String, Name = "Resolution", LongName = "Texture Resolution" },
                new PropertyDefinition { Type = PropertyTypeUtil.FromCustom(TextureProperty.SizeOnDisk), Format = PropertyFormat.Bytes, Name = "Size", LongName = "Texture Size" },
                new PropertyDefinition { Type = PropertyTypeUtil.FromCustom(TextureProperty.StreamingMipMap), Format = PropertyFormat.Bool, Name = "Streaming", LongName = "Mipmaps Streaming" },
                new PropertyDefinition { Type = PropertyType.Path, Name = "Path", MaxAutoWidth = 500 }
            }
        };

        public override string Name => "Textures";

        public override IReadOnlyCollection<IssueLayout> SupportedLayouts => new IssueLayout[]
        {
            k_TextureLayout,
            AssetsModule.k_IssueLayout
        };

        const string k_TextureStreamingMipmapsSizeLimit = "TextureStreamingMipmapsSizeLimit";
        const string k_TextureSizeLimit                 = "TextureSizeLimit";
        const string k_SpriteAtlasEmptySpaceLimit       = "SpriteAtlasEmptySpaceLimit";

        public override void RegisterParameters(DiagnosticParams diagnosticParams)
        {
            diagnosticParams.RegisterParameter(k_TextureStreamingMipmapsSizeLimit, 4000);
            diagnosticParams.RegisterParameter(k_TextureSizeLimit, 2048);
            diagnosticParams.RegisterParameter(k_SpriteAtlasEmptySpaceLimit, 50);
        }

        public override AnalysisResult Audit(AnalysisParams analysisParams, IProgress progress = null)
        {
            var analyzers = GetPlatformAnalyzers(analysisParams.Platform);

            var diagnosticParams = analysisParams.DiagnosticParams;
            var textureStreamingMipmapsSizeLimit = diagnosticParams.GetParameter(k_TextureStreamingMipmapsSizeLimit);
            var textureSizeLimit = diagnosticParams.GetParameter(k_TextureSizeLimit);
            var spriteAtlasEmptySpaceLimit = diagnosticParams.GetParameter(k_SpriteAtlasEmptySpaceLimit);
            var platformString = analysisParams.PlatformString;

            var context = new TextureAnalysisContext
            {
                // Importer set in loop
                // ImporterPlatformSettings set in loop
                // Texture set in loop
                Params = analysisParams,
                TextureStreamingMipmapsSizeLimit = textureStreamingMipmapsSizeLimit,
                TextureSizeLimit = textureSizeLimit,
                SpriteAtlasEmptySpaceLimit = spriteAtlasEmptySpaceLimit
            };

            var assetPaths = GetAssetPathsByFilter("t:texture, a:assets", context);

            progress?.Start("Finding Textures", "Search in Progress...", assetPaths.Length);

            foreach (var assetPath in assetPaths)
            {
                if (progress?.IsCancelled ?? false)
                    return AnalysisResult.Cancelled;

                var textureImporter = AssetImporter.GetAtPath(assetPath) as TextureImporter;
                if (textureImporter == null)
                {
                    continue; // skip render textures
                }

                context.Importer = textureImporter;
                context.ImporterPlatformSettings = textureImporter.GetPlatformTextureSettings(platformString);
                context.Texture = AssetDatabase.LoadAssetAtPath<Texture>(assetPath);

                if (string.IsNullOrEmpty(context.Texture.name))
                    context.Name = Path.GetFileNameWithoutExtension(assetPath);
                else
                    context.Name = context.Texture.name;

                foreach (var analyzer in analyzers)
                {
                    analysisParams.OnIncomingIssues(analyzer.Analyze(context));
                }

                progress?.Advance();
            }

            progress?.Clear();

            return AnalysisResult.Success;
        }
    }
}
