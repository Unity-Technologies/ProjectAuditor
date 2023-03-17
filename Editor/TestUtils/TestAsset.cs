using System;
using System.IO;
using NUnit.Framework;
using Unity.ProjectAuditor.Editor.Utils;
using UnityEditor;

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
