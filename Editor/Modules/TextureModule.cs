using System;
using System.Collections.Generic;
using System.Linq;
using Unity.ProjectAuditor.Editor.Core;
using Unity.ProjectAuditor.Editor.Diagnostic;
using UnityEngine.Profiling;
using UnityEditor;
using UnityEngine;

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
        Platform,
        Num
    }

    public enum PlatformTextureProperty
    {
        Format,
        Platform,
        Num
    }


    class TextureModule : ProjectAuditorModule
    {
        private static readonly IssueLayout k_TexturesIssueLayout = new IssueLayout
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
                new PropertyDefinition { type = PropertyType.Path, name = "Path"}
            }
        };

        static readonly IssueLayout k_PlatformTextureIssueLayout = new IssueLayout
        {
            category = IssueCategory.PlatformTexture,
            properties = new[]
            {
                new PropertyDefinition { type = PropertyType.Description, format = PropertyFormat.String, name = "Name", longName = "Texture Name" },
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(PlatformTextureProperty.Format), format = PropertyFormat.String, name = "Format", longName = "Compression Format" },
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(PlatformTextureProperty.Platform), format = PropertyFormat.String, name = "Platform" },
                new PropertyDefinition { type = PropertyType.Path, name = "Path"}
            }
        };

        public override string name => "Textures";

        public override bool isEnabledByDefault => false;

        List<ITextureAnalyzer> m_Analyzers;
        HashSet<Descriptor> m_DiagnosticDescriptors;

        public override IReadOnlyCollection<IssueLayout> supportedLayouts => new IssueLayout[]
        {
            k_TexturesIssueLayout,
            k_PlatformTextureIssueLayout
        };

        public override IReadOnlyCollection<Descriptor> supportedDescriptors => m_DiagnosticDescriptors;

        public override void Initialize(ProjectAuditorConfig config)
        {
            m_Analyzers = new List<ITextureAnalyzer>();
            m_DiagnosticDescriptors = new HashSet<Descriptor>();

            foreach (var type in TypeCache.GetTypesDerivedFrom(typeof(ITextureAnalyzer)))
                AddAnalyzer(Activator.CreateInstance(type) as ITextureAnalyzer);
        }

        public override void Audit(ProjectAuditorParams projectAuditorParams, IProgress progress = null)
        {
            var allTextures = AssetDatabase.FindAssets("t: Texture, a:assets");
            var issues = new List<ProjectIssue>();
            var currentPlatform = projectAuditorParams.platform;
            var currentPlatformString = projectAuditorParams.platform.ToString();

            progress?.Start("Finding Textures", "Search in Progress...", allTextures.Length);

            foreach (var guid in allTextures)
            {
                var pathToTexture = AssetDatabase.GUIDToAssetPath(guid);
                var textureImporter = AssetImporter.GetAtPath(pathToTexture) as TextureImporter;
                if (textureImporter == null)
                {
                    continue; // skip render textures
                }

                var texture = AssetDatabase.LoadAssetAtPath<Texture>(pathToTexture);
                var size = Profiler.GetRuntimeMemorySizeLong(texture);
                var platformSettings = textureImporter.GetPlatformTextureSettings(currentPlatformString);

                var resolution = (texture.width + "x" + texture.height);

                var issue = ProjectIssue.Create(k_TexturesIssueLayout.category, texture.name)
                    .WithCustomProperties(
                        new object[((int)TextureProperty.Num)]
                        {
                            textureImporter.textureShape,
                            textureImporter.textureType,
                            platformSettings.format,
                            platformSettings.textureCompression,
                            textureImporter.mipmapEnabled,
                            textureImporter.isReadable,
                            resolution,
                            size,
                            currentPlatform
                        })
                    .WithLocation(new Location(pathToTexture));

                issues.Add(issue);

                foreach (var analyzer in m_Analyzers)
                {
                    var platformDiagnostics = analyzer.Analyze(currentPlatform, texture, platformSettings).ToArray();

                    issues.AddRange(platformDiagnostics);
                }

                progress?.Advance();
            }


            if (issues.Count > 0)
                projectAuditorParams.onIncomingIssues(issues);

            progress?.Clear();

            projectAuditorParams.onModuleCompleted?.Invoke();
        }

        void AddAnalyzer(ITextureAnalyzer analyzer)
        {
            analyzer.Initialize(this);
            m_Analyzers.Add(analyzer);
        }
    }
}
