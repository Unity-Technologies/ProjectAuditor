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
        Format,
        Shape,
        ReadWrite,
        StreamingMipMaps,
        MinMipMapLevel,
        Resolution,
        SizeOnDisk,
        Path,
        Num
    }

    class TextureModule : ProjectAuditorModule
    {
        public static string[] searchTheseFolders;


        static readonly IssueLayout k_IssueLayout = new IssueLayout
        {
            category = IssueCategory.Texture,
            properties = new[]
            {   new PropertyDefinition { type = Editor.PropertyType.Description, name = "Texture Description", longName = "Textures Description" },
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(TextureProperties.Format), format = PropertyFormat.String, name = "Format", longName = "Texture Format" },
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(TextureProperties.Shape), format = PropertyFormat.String, name = "TextureShape", longName = "Texture Shape" },
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(TextureProperties.ReadWrite), format = PropertyFormat.Bool, name = "Readable", longName = "Readable" },
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(TextureProperties.StreamingMipMaps), format = PropertyFormat.String, name = "StreamingMipMaps", longName = "Texture StreamingMipMaps" },
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(TextureProperties.MinMipMapLevel), format = PropertyFormat.String, name = "MinMipMapLevel", longName = "Texture MinMipMapLevel" },
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(TextureProperties.SizeOnDisk), format = PropertyFormat.String, name = "Size", longName = "Texture Size" },
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
            var issues = new List<ProjectIssue>();

            foreach (string aTexture in allTextures)
            {
                var path = AssetDatabase.GUIDToAssetPath(aTexture);
                var t = (Texture2D)AssetDatabase.LoadAssetAtPath(path, typeof(Texture2D));
                var tSize = Profiler.GetRuntimeMemorySizeLong(t);

                var issue = ProjectIssue.Create(k_IssueLayout.category, t.name)
                    .WithCustomProperties(new object[((int)TextureProperties.Num)]
                    {
                        t.format, //Format
                        t.dimension, //Shape
                        t.isReadable, //Read-Write
                        t.streamingMipmaps, //Streaming Mip Maps
                        t.minimumMipmapLevel, // Minimum MipMap Level
                        (t.width + "x" + t.height), // Resolution
                        Utils.Formatting.FormatSize((ulong)tSize),
                        path, //Location
                    });

                issues.Add(issue);
            }

            if (issues.Count > 0)
                projectAuditorParams.onIncomingIssues(issues);

            projectAuditorParams.onModuleCompleted.Invoke();
        }
    }
}
