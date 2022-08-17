using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Profiling;
using Unity.ProjectAuditor.Editor;
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
        public static string[] searchTheseFolders;
        public TextureImporter TextProp;

        private static readonly IssueLayout k_IssueLayout = new IssueLayout
        {
            category = IssueCategory.Texture,
            properties = new[]
            {
                new PropertyDefinition {type = PropertyTypeUtil.FromCustom(TextureProperties.Name), format = PropertyFormat.String, name = "Name", longName = "Texture Name" },
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(TextureProperties.Shape), format = PropertyFormat.String, name = "TextureShape", longName = "Texture Shape" },
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(TextureProperties.ImporterType), format = PropertyFormat.String, name = "Importer Type", longName = "Texture Importer Type" },
                new PropertyDefinition {type = PropertyTypeUtil.FromCustom(TextureProperties.Format), format = PropertyFormat.String, name = "Format", longName = "Texture Format" },
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(TextureProperties.TextureCompression), format = PropertyFormat.String, name = "Compression Used?", longName = "Texture Compression" },
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(TextureProperties.MipMapEnabled), format = PropertyFormat.Bool, name = "MipMaps", longName = "Texture MipMap Used?" },
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(TextureProperties.Readable), format = PropertyFormat.Bool, name = "Readable", longName = "Readable" },
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(TextureProperties.Resolution), format = PropertyFormat.String, name = "Resolution", longName = "Texture Resolution" },
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(TextureProperties.SizeOnDisk), format = PropertyFormat.String, name = "Size", longName = "Texture Size" },
            }
        };
        public override bool IsEnabledByDefault() { return false; }

        public override IEnumerable<IssueLayout> GetLayouts() {  yield return k_IssueLayout;  }

        public override void Audit(ProjectAuditorParams projectAuditorParams, IProgress progress = null)
        {
            var allTextures = AssetDatabase.FindAssets("t: Texture, a:assets");
            var issues = new List<ProjectIssue>();
            progress?.Start("Finding Textures", "Positive Always", allTextures.Length);

            foreach (string aTexture in allTextures)
            {
                var path = AssetDatabase.GUIDToAssetPath(aTexture);
                var tName = ((Texture2D)AssetDatabase.LoadAssetAtPath(path, typeof(Texture2D)));
                var t = (TextureImporter)TextureImporter.GetAtPath(path);
                var tSize = Profiler.GetRuntimeMemorySizeLong(t);

                TextureImporter textureImporter = AssetImporter.GetAtPath(path) as TextureImporter;

                var issue = ProjectIssue.Create(k_IssueLayout.category, tName.name).WithCustomProperties(new object[((int)TextureProperties.Num)]
                {
                    tName.name,
                    textureImporter.textureShape,
                    textureImporter.textureType, //Importer Type
                    t.GetPlatformTextureSettings("Android").format,     //new Format
                    t.GetPlatformTextureSettings("Android").textureCompression, //new TextureCompression
                    textureImporter.mipmapEnabled,
                    textureImporter.isReadable,
                    #if UNITY_2021_2_OR_NEWER
                    t.GetSourceTextureWidthAndHeight.width + "x" + t.GetSourceTextureWidthAndHeight.height, //Not avail before Unity 2021.2
                   #else
                    (tName.width + "x" + tName.height),
                   #endif
                    Utils.Formatting.FormatSize((ulong)tSize),
                });

                issues.Add(issue);

                progress?.Advance();
            }

            if (issues.Count > 0)
                projectAuditorParams.onIncomingIssues(issues);
            progress?.Clear();

            projectAuditorParams.onModuleCompleted.Invoke();
        }
    }
}