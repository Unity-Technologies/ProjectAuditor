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
        public static string[] searchTheseFolders;                 // Manually Choose Folders to do the FindAssets search within.


        static readonly IssueLayout k_IssueLayout = new IssueLayout
        {
            category = IssueCategory.Texture,
            properties = new[]
            {
                // new PropertyDefinition { type = PropertyType.Description, name = "Texture Analysis", longName = "Results of Texture Analysis "},
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(TextureProperties.Name), format = PropertyFormat.String, name = "Name", longName = "Show Texture Name" },
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(TextureProperties.Format), format = PropertyFormat.String, name = "Format", longName = "Show Texture Format" },
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(TextureProperties.Shape), format = PropertyFormat.String, name = "Shape", longName = "Show Texture Shape" },
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(TextureProperties.ReadWrite), format = PropertyFormat.String, name = "Writeable", longName = "Show Texture ReadWrite" },
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(TextureProperties.StreamingMipMaps), format = PropertyFormat.String, name = "StreamingMipMaps", longName = "Show Texture StreamingMipMaps" },
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(TextureProperties.MinMipMapLevel), format = PropertyFormat.String, name = "MinMipMapLevel", longName = "Show Texture MinMipMapLevel" },
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(TextureProperties.Resolution), format = PropertyFormat.String, name = "Resolution", longName = "Show Texture Resolution" },
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(TextureProperties.SizeOnDisk), format = PropertyFormat.String, name = "SizeOnDisk", longName = "Show Texture SizeOnDisk" },
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(TextureProperties.Path), format = PropertyFormat.String, name = "Location", longName = "Show Texture Location Path" }
                //  new PropertyDefinition { type = PropertyType.Area, name = "Texture Recommendations", longName = "Recommendations for Optimizations for the Textures in the project." }
                // above line makes stuff puke while the module is half-implemented
            }
        };

        public override IEnumerable<IssueLayout> GetLayouts()
        {
            yield return k_IssueLayout;
        }

        public override void Audit(ProjectAuditorParams projectAuditorParams, IProgress progress = null)
        {
            // Implement your analysis here
            var allTextures = AssetDatabase.FindAssets("t: Texture, a:assets");  // t: filter is for type of asset returned and a: filter is for location to search (only this location)
            // Debug.Log("Locations Searched: " + String.Join(" & ", new List<string>(searchTheseFolders).ConvertAll(i => i.ToString())));
            // Create an issue
            var issues = new List<ProjectIssue>();


            // Grab the texture's path/location and add it as an issue.
            foreach (string aTexture in allTextures)    // ACTUALLY for each texture referenced....
            {
                var path = AssetDatabase.GUIDToAssetPath(aTexture);
                Texture2D t = (Texture2D)AssetDatabase.LoadAssetAtPath(path, typeof(Texture2D));   // This uses the pre-grabbed Texture (string) name, & makes t reference the actual texture, (Texture2D) not a string.
                double tSize = Profiler.GetRuntimeMemorySizeLong(t); //For display of the file size, later


                var issue = ProjectIssue.Create(k_IssueLayout.category, t.name)
                    .WithCustomProperties(new object[((int)TextureProperties.Num)]
                    {
                        t.name, //Name
                        t.format, //Format
                        t.dimension, //Shape
                        t.isReadable, //Read-Write
                        t.streamingMipmaps, //Streaming Mip Maps
                        t.minimumMipmapLevel, // Minimum MipMap Level
                        (t.width + "x" + t.height), // Resolution
                        GetFileSize(tSize),
                        path, //Location
                    });

                issues.Add(issue);
                // issues.Add(ProjectIssue.Create(k_IssueLayout.category, ("Location of Texture: " + path)));

                // add more issues...
            }

            if (issues.Count > 0)
                projectAuditorParams.onIncomingIssues(issues);

            // Notify that the analysis of this module is completed
            projectAuditorParams.onModuleCompleted.Invoke();
        }

        private string GetFileSize(double byteCount) //Used to nicely display the file sizes in a readable format, reflecting KB, MB, GB properly
        {
            string size = "0 to start";
            if (byteCount >= 1073741824.0)
                size = String.Format("{0:##.##}", byteCount / 1073741824.0) + " GB";
            else if (byteCount >= 1048576.0)
                size = String.Format("{0:##.##}", byteCount / 1048576.0) + " MB";
            else if (byteCount >= 1024.0)
                size = String.Format("{0:##.##}", byteCount / 1024.0) + " KB";
            else if (byteCount > 0 && byteCount < 1024.0)
                size = byteCount.ToString() + " Bytes";

            return size;
        }
    }
}
