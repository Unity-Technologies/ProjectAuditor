using System;
using System.IO;
using NUnit.Framework;
using Unity.ProjectAuditor.Editor.TestUtils;
using UnityEditor;

namespace Unity.ProjectAuditor.EditorTests
{
    class ConfigAssetTests : TestFixtureBase
    {
        [Test]
        public void ConfigAsset_DefaultAsset_IsCreated()
        {
            Assert.IsTrue(File.Exists(Unity.ProjectAuditor.Editor.ProjectAuditor.DefaultAssetPath));
        }

        [Test]
        public void ConfigAsset_CustomAsset_IsCreated()
        {
            var assetPath = Path.Combine(TestAsset.TempAssetsFolder, "MyConfig.asset");
            new Unity.ProjectAuditor.Editor.ProjectAuditor(assetPath);
            Assert.True(File.Exists(assetPath));

            AssetDatabase.DeleteAsset(assetPath);
            Assert.False(File.Exists(assetPath));
        }
    }
}
