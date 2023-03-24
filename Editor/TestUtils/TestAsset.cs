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

        public readonly string relativePath;

        public string fileName
        {
            get { return Path.GetFileName(relativePath); }
        }

        TestAsset(string fileName)
        {
            relativePath = PathUtils.Combine(TempAssetsFolder, fileName);

            if (!File.Exists(relativePath))
                Directory.CreateDirectory(Path.GetDirectoryName(relativePath));
        }

        public TestAsset(string fileName, string content) :
            this(fileName)
        {
            File.WriteAllText(relativePath, content);

            Assert.True(File.Exists(relativePath));

            AssetDatabase.ImportAsset(relativePath, ImportAssetOptions.ForceUpdate);
        }

        public TestAsset(string fileName, byte[] byteContent) :
            this(fileName)
        {
            File.WriteAllBytes(relativePath, byteContent);

            Assert.True(File.Exists(relativePath));
            AssetDatabase.ImportAsset(relativePath, ImportAssetOptions.ForceUpdate);
        }

        public static TestAsset Save(UnityEngine.Object asset, string fileName)
        {
            var tempAsset = new TestAsset(fileName);
            AssetDatabase.CreateAsset(asset, tempAsset.relativePath);
            AssetDatabase.ImportAsset(tempAsset.relativePath, ImportAssetOptions.ForceUpdate);

            return tempAsset;
        }

        //SpriteAtlasAsset Save is not compatible with the AssetDatabase save
        //Alternative function to create a TestAsset from a SpriteAtlas
        public static TestAsset SaveSpriteAtlasAsset(SpriteAtlasAsset asset, string fileName)
        {
            var tempAsset = new TestAsset(fileName);
#if UNITY_2021_1_OR_NEWER
            SpriteAtlasAsset.Save(asset, tempAsset.relativePath);
#else
            if (asset == null)
                throw new ArgumentNullException("Parameter asset is null");
            UnityEditorInternal.InternalEditorUtility.SaveToSerializedFileAndForget(new UnityEngine.Object[1]
            {
                asset
            }, tempAsset.relativePath, EditorSettings.serializationMode != SerializationMode.ForceBinary);

#endif
            AssetDatabase.ImportAsset(tempAsset.relativePath, ImportAssetOptions.ForceUpdate);

            return tempAsset;
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
