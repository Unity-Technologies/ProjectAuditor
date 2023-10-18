using System;
using System.IO;
using NUnit.Framework;
using Unity.ProjectAuditor.Editor;
using Unity.ProjectAuditor.Editor.Tests.Common;
using UnityEditor;

namespace Unity.ProjectAuditor.EditorTests
{
    class RulesAssetTests : TestFixtureBase
    {
        [Test]
        public void RulesAsset_DefaultAsset_IsCreated()
        {
            Assert.IsTrue(File.Exists(Editor.UserPreferences.rulesAssetPath));
        }

        [Test]
        public void RulesAsset_CustomAsset_IsCreated()
        {
            var assetPath = Path.Combine(TestAsset.TempAssetsFolder, "MyRules.asset");
            new ProjectAuditorParams(assetPath);
            Assert.True(File.Exists(assetPath));

            AssetDatabase.DeleteAsset(assetPath);
            Assert.False(File.Exists(assetPath));
        }
    }
}
