using System.Collections.Generic;
using Unity.ProjectAuditor.Editor.Core;
using UnityEngine.Profiling;
using UnityEditor;
using UnityEngine;

namespace Unity.ProjectAuditor.Editor.Modules
{
    public enum TextureProperties
    {
        Name,
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
        private static readonly IssueLayout k_TexturesIssueLayout = new IssueLayout
        {
            category = IssueCategory.Texture,
            properties = new[]
            {
                new PropertyDefinition {type = PropertyTypeUtil.FromCustom(TextureProperties.Name), format = PropertyFormat.String, name = "Name", longName = "Texture Name" },
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(TextureProperties.Shape), format = PropertyFormat.String, name = "Shape", longName = "Texture Shape" },
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(TextureProperties.ImporterType), format = PropertyFormat.String, name = "Importer Type", longName = "Texture Importer Type" },
                new PropertyDefinition {type = PropertyTypeUtil.FromCustom(TextureProperties.Format), format = PropertyFormat.String, name = "Format", longName = "Texture Format" },
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(TextureProperties.TextureCompression), format = PropertyFormat.String, name = "Compression", longName = "Texture Compression" },
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(TextureProperties.MipMapEnabled), format = PropertyFormat.Bool, name = "MipMaps", longName = "Texture MipMaps Enabled" },
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(TextureProperties.Readable), format = PropertyFormat.Bool, name = "Readable", longName = "Readable" },
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(TextureProperties.Resolution), format = PropertyFormat.String, name = "Resolution", longName = "Texture Resolution" },
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(TextureProperties.SizeOnDisk), format = PropertyFormat.Bytes, name = "Size", longName = "Texture Size" },
                new PropertyDefinition { type = PropertyType.Path, name = "Path"}
            }
        };
        public override string name => "Textures";

        public override IReadOnlyCollection<IssueLayout> supportedLayouts => new IssueLayout[]
        {
            k_TexturesIssueLayout,
        };


        public override void Audit(ProjectAuditorParams projectAuditorParams, IProgress progress = null)
        {
            var allTextures = AssetDatabase.FindAssets("t: Texture, a:assets");
            var issues = new List<ProjectIssue>();
            progress?.Start("Finding Textures", "Search in Progress...", allTextures.Length);

            foreach (var aTexture in allTextures)
            {
                var pathToTexture = AssetDatabase.GUIDToAssetPath(aTexture);
                var location = new Location(pathToTexture);

                var t = AssetImporter.GetAtPath(pathToTexture) as TextureImporter;
                if (t == null) { continue; }

                var tName = (Texture)AssetDatabase.LoadAssetAtPath(pathToTexture, typeof(Texture));
                var tSize = Profiler.GetRuntimeMemorySizeLong(tName);

                var issue = ProjectIssue.Create(k_TexturesIssueLayout.category, tName.name).WithCustomProperties(new object[((int)TextureProperties.Num)]
                {
                    tName.name,
                    t.textureShape,
                    t.textureType,
                    t.GetPlatformTextureSettings("Android").format,
                    t.GetPlatformTextureSettings("Android").textureCompression,
                    t.mipmapEnabled,
                    t.isReadable,
                        #if UNITY_2021_2_OR_NEWER
                    t.GetSourceTextureWidthAndHeight.width + "x" + t.GetSourceTextureWidthAndHeight.height,         //Not avail before Unity 2021.2
                        #else
                    (tName.width + "x" + tName.height),
                        #endif
                    tSize
                })
                    .WithLocation(location);
                issues.Add(issue);

                progress?.Advance();
            }

            if (issues.Count > 0)
                projectAuditorParams.onIncomingIssues(issues);
            progress?.Clear();

            projectAuditorParams.onModuleCompleted?.Invoke();
        }

        public override bool isEnabledByDefault => false;
    }
}