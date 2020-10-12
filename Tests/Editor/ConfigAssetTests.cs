using System.IO;
using NUnit.Framework;

namespace UnityEditor.ProjectAuditor.EditorTests
{
    public class ConfigAssetTests
    {
        [Test]
        public void DefaultConfigAssetIsCreated()
        {
            new Unity.ProjectAuditor.Editor.ProjectAuditor();
            Assert.IsTrue(File.Exists(Unity.ProjectAuditor.Editor.ProjectAuditor.DefaultAssetPath));
        }

        [Test]
        public void CustomConfigAssetIsCreated()
        {
            var assetPath = "Assets/Editor/MyConfig.asset";
            new Unity.ProjectAuditor.Editor.ProjectAuditor(assetPath);
            Assert.IsTrue(File.Exists(assetPath));
        }
    }
}
