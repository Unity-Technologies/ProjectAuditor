using System.IO;
using NUnit.Framework;

namespace UnityEditor.ProjectAuditor.EditorTests
{
    public class ConfigAssetTests
    {
        [Test]
        public void CustomConfigAssetIsCreated()
        {
            var assetPath = "Assets/Editor/MyConfig.asset";
            new Unity.ProjectAuditor.Editor.ProjectAuditor(assetPath);
            Assert.IsTrue(File.Exists(assetPath));
        }
    }
}
