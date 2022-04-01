using System;
using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.VersionControl;

namespace Unity.ProjectAuditor.EditorTests
{
    class ConfigAssetTests
    {
        [Test]
        public void ConfigAsset_DefaultAsset_IsCreated()
        {
            new Unity.ProjectAuditor.Editor.ProjectAuditor();
            Assert.IsTrue(File.Exists(Unity.ProjectAuditor.Editor.ProjectAuditor.DefaultAssetPath));
        }

        [Test]
        public void ConfigAsset_CustomAsset_IsCreated()
        {
            var assetPath = "Assets/Editor/MyConfig.asset";
            new Unity.ProjectAuditor.Editor.ProjectAuditor(assetPath);
            Assert.True(File.Exists(assetPath));

            AssetDatabase.DeleteAsset(assetPath);
            Assert.False(File.Exists(assetPath));
        }
    }
}
