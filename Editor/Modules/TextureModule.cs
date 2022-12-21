using System;
using System.Collections.Generic;
using System.Linq;
using Unity.ProjectAuditor.Editor.Core;
using Unity.ProjectAuditor.Editor.Diagnostic;
using Unity.ProjectAuditor.Editor.Utils;
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
        Num
    }

    class TextureModule : ProjectAuditorModule
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
                new PropertyDefinition { type = PropertyType.Path, name = "Path"}
            }
        };

        public override string name => "Textures";

        public override bool isEnabledByDefault => false;

        List<ITextureModuleAnalyzer> m_Analyzers;

        public override IReadOnlyCollection<IssueLayout> supportedLayouts => new IssueLayout[]
        {
            k_TextureLayout,
            AssetsModule.k_IssueLayout
        };

        public override void Initialize(ProjectAuditorConfig config)
        {
            m_Analyzers = new List<ITextureModuleAnalyzer>();
            m_Descriptors = new HashSet<Descriptor>();

            foreach (var type in TypeCache.GetTypesDerivedFrom(typeof(ITextureModuleAnalyzer)))
                AddAnalyzer(Activator.CreateInstance(type) as ITextureModuleAnalyzer);
        }

        public override void Audit(ProjectAuditorParams projectAuditorParams, IProgress progress = null)
        {
            var analyzers = m_Analyzers.Where(a => CoreUtils.SupportsPlatform(a.GetType(), projectAuditorParams.platform)).ToArray();
            var allTextures = AssetDatabase.FindAssets("t:texture, a:assets");
            var issues = new List<ProjectIssue>();
            var currentPlatform = projectAuditorParams.platform;
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

                var texture = AssetDatabase.LoadAssetAtPath<Texture>(assetPath);

                // TODO: the size returned by the profiler is not the exact size on the target platform. Needs to be fixed.
                var size = Profiler.GetRuntimeMemorySizeLong(texture);
                var platformSettings = textureImporter.GetPlatformTextureSettings(currentPlatformString);

                var resolution = texture.width + "x" + texture.height;

                var issue = ProjectIssue.Create(k_TextureLayout.category, texture.name)
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

                issues.Add(issue);
                issues.AddRange(analyzers.SelectMany(a => a.Analyze(currentPlatform, textureImporter, platformSettings)));

                progress?.Advance();
            }

            if (issues.Count > 0)
                projectAuditorParams.onIncomingIssues(issues);

            progress?.Clear();

            projectAuditorParams.onModuleCompleted?.Invoke();
        }

        void AddAnalyzer(ITextureModuleAnalyzer moduleAnalyzer)
        {
            moduleAnalyzer.Initialize(this);
            m_Analyzers.Add(moduleAnalyzer);
        }
    }
}
