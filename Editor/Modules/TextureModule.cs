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
        Format,


        /*
        StreamingMipMaps,
        MinMipMapLevel,
        Resolution,
        SizeOnDisk, */
        TextureCompression,
        Readable,
        Shape,
        ImporterType,
        Path,

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
                //  new PropertyDefinition { type = Editor.PropertyType.Description, format = PropertyFormat.String, name = "Texture Description", longName = "Textures Description" },
                new PropertyDefinition {type = PropertyTypeUtil.FromCustom(TextureProperties.Name), format = PropertyFormat.String, name = "Name", longName = "Texture Name" },
                new PropertyDefinition {type = PropertyTypeUtil.FromCustom(TextureProperties.Format), format = PropertyFormat.String, name = "Format", longName = "Texture Format" },
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(TextureProperties.TextureCompression), format = PropertyFormat.String, name = "Compression Used?", longName = "Texture Compression" },
                /*


                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(TextureProperties.StreamingMipMaps), format = PropertyFormat.Bool, name = "StreamingMipMaps", longName = "Texture StreamingMipMaps" },
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(TextureProperties.MinMipMapLevel), format = PropertyFormat.Integer, name = "MinMipMapLevel", longName = "Texture MinMipMapLevel" },
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(TextureProperties.SizeOnDisk), format = PropertyFormat.String, name = "Size", longName = "Texture Size" }, }
                 */
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(TextureProperties.ImporterType), format = PropertyFormat.String, name = "Importer Type", longName = "Texture Importer Type" },
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(TextureProperties.Shape), format = PropertyFormat.String, name = "TextureShape", longName = "Texture Shape" },
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(TextureProperties.Readable), format = PropertyFormat.Bool, name = "Readable", longName = "Readable" },
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(TextureProperties.Path), format = PropertyFormat.String, name = "Location", longName = "Show Texture Location Path" },
            }
        };
        public override bool IsEnabledByDefault()
        {
            return false;
        }

        public override IEnumerable<IssueLayout> GetLayouts()
        {
            yield return k_IssueLayout;
        }

        public override void Audit(ProjectAuditorParams projectAuditorParams, IProgress progress = null)
        {
            var allTextures = AssetDatabase.FindAssets("t: Texture, a:assets");
            progress?.Start("Finding Textures", "Positive Always", allTextures.Length);
            var issues = new List<ProjectIssue>();

            foreach (string aTexture in allTextures)
            {
                var path = AssetDatabase.GUIDToAssetPath(aTexture);
                var tname = ((Texture2D)AssetDatabase.LoadAssetAtPath(path, typeof(Texture2D)));
                var t = (TextureImporter)TextureImporter.GetAtPath(path);
                var tSize = Profiler.GetRuntimeMemorySizeLong(t);
                //TextureImporter textureImporter = AssetImporter.GetAtPath(path) as TextureImporter;
                var issue = ProjectIssue.Create(k_IssueLayout.category, tname.name).WithCustomProperties(new object[((int)TextureProperties.Num)]
                {
                    tname.name, // must use this way, texture name is not available from ImportSettings or GetPlatformSettings
                    t.GetPlatformTextureSettings("Android").format,     //new Format
                    t.GetPlatformTextureSettings("Android").textureCompression, //new TextureCompression
                    t.isReadable,
                    t.textureShape,
                    t.textureType,
                    path,  //new

                    /*
                    t.dimension,     //Shape
                    t.isReadable,     //Read-Write
                    t.streamingMipmaps,     //Streaming Mip Maps
                    t.minimumMipmapLevel,     // Minimum MipMap Level
                    (t.width + "x" + t.height),     // Resolution
                    Utils.Formatting.FormatSize((ulong)tSize),
                    AssetDatabase.GetAssetPath(t),     //Location
                    */
                });
                //      Debug.Log("Texture location is reported as: " + path);

                issues.Add(issue);

                progress?.Advance();
            }

            if (issues.Count > 0)
                projectAuditorParams.onIncomingIssues(issues);
            progress?.Clear();


            /*
                string  platformString = "Android";
                int     platformMaxTextureSize = 0;
                TextureImporterFormat platformTextureFmt;
                int     platformCompressionQuality = 0;
                bool    platformAllowsAlphaSplit = false;

                TextureImporter ti = (TextureImporter)TextureImporter.GetAtPath("Assets/characters.png");
                if (ti.GetPlatformTextureSettings(platformString, out platformMaxTextureSize, out platformTextureFmt, out platformCompressionQuality, out platformAllowsAlphaSplit))
                {
                    Debug.Log("Texture Info For platform: " + platformString + "are as follows : /n" + "Format: " + platformTextureFmt + " /n " + "MaxTextureSize : " + platformMaxTextureSize
                      + " /n " + "Texture Size : " + platformTextureFmt  + " /n" + "Compression Quality : " + platformCompressionQuality + " /n " +  " /n " + "AllowsAlphaSplit : " + platformAllowsAlphaSplit  +  TextureImporter.isReadable
                    TextureImporter.textureShape );
                }

                TextureImporter.textureShape
            */
            projectAuditorParams.onModuleCompleted.Invoke();
        }
    }
}
