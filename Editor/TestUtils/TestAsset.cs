using System;
using System.IO;
using NUnit.Framework;
using Unity.ProjectAuditor.Editor.Utils;
using UnityEditor;
using UnityEditor.U2D;

namespace Unity.ProjectAuditor.Editor.Tests.Common
{
    public class TestAsset
    {
        public static readonly string TempAssetsFolder = PathUtils.Combine("Assets", "ProjectAuditor-Temp");

        public readonly string RelativePath;

        public string FileName => Path.GetFileName(RelativePath);

        TestAsset(string fileName)
        {
            RelativePath = PathUtils.Combine(TempAssetsFolder, fileName);

            if (!File.Exists(RelativePath))
                Directory.CreateDirectory(Path.GetDirectoryName(RelativePath));
        }

        public TestAsset(string fileName, string content) :
            this(fileName)
        {
            File.WriteAllText(RelativePath, content);

            Assert.True(File.Exists(RelativePath));

            AssetDatabase.ImportAsset(RelativePath, ImportAssetOptions.ForceUpdate);
        }

        public TestAsset(string fileName, byte[] byteContent) :
            this(fileName)
        {
            File.WriteAllBytes(RelativePath, byteContent);

            Assert.True(File.Exists(RelativePath));
            AssetDatabase.ImportAsset(RelativePath, ImportAssetOptions.ForceUpdate);
        }

        public void CleanupLocal()
        {
            if (File.Exists(RelativePath))
            {
                AssetDatabase.DeleteAsset(RelativePath);
                AssetDatabase.Refresh();
            }
        }

        public static TestAsset Save(UnityEngine.Object asset, string fileName)
        {
            var tempAsset = new TestAsset(fileName);
            AssetDatabase.CreateAsset(asset, tempAsset.RelativePath);
            AssetDatabase.ImportAsset(tempAsset.RelativePath, ImportAssetOptions.ForceUpdate);

            return tempAsset;
        }

        //SpriteAtlasAsset Save is not compatible with the AssetDatabase save
        //Alternative function to create a TestAsset from a SpriteAtlas
        public static TestAsset SaveSpriteAtlasAsset(SpriteAtlasAsset asset, string fileName)
        {
            var tempAsset = new TestAsset(fileName);
            #if UNITY_2021_1_OR_NEWER
            SpriteAtlasAsset.Save(asset, tempAsset.RelativePath);
            #else
            if (asset == null)
                throw new ArgumentNullException("Parameter asset is null");
            UnityEditorInternal.InternalEditorUtility.SaveToSerializedFileAndForget(new UnityEngine.Object[1]
            {
                asset
            }, tempAsset.RelativePath, EditorSettings.serializationMode != SerializationMode.ForceBinary);
            #endif
            AssetDatabase.ImportAsset(tempAsset.RelativePath, ImportAssetOptions.ForceUpdate);

            return tempAsset;
        }

        public static void CreateTempFolder()
        {
            if (!Directory.Exists(TempAssetsFolder))
                Directory.CreateDirectory(TempAssetsFolder);
        }

        public static void Cleanup()
        {
            if (Directory.Exists(TempAssetsFolder))
            {
                AssetDatabase.DeleteAsset(TempAssetsFolder);
                AssetDatabase.Refresh();
            }
        }
    }
}
