using System;
using System.IO;
using NUnit.Framework;

namespace UnityEditor.ProjectAuditor.EditorTests
{
    public class TempAsset
    {
        const string TempFolder = "ProjectAuditor-Temp";

        public TempAsset(string scriptName, string content)
        {
            relativePath = Path.Combine("Assets", Path.Combine(TempFolder, scriptName)).Replace("\\", "/");
            if (!File.Exists(relativePath))
                Directory.CreateDirectory(Path.GetDirectoryName(relativePath));

            File.WriteAllText(relativePath, content);

            Assert.True(File.Exists(relativePath));

            AssetDatabase.ImportAsset(relativePath, ImportAssetOptions.ForceUpdate);
        }

        public readonly string relativePath;

        public string scriptName
        {
            get { return Path.GetFileName(relativePath); }
        }

        public static void Cleanup()
        {
            var path = Path.Combine("Assets", TempFolder);
            Directory.Delete(path, true);
            File.Delete(path + ".meta");
            AssetDatabase.Refresh();
        }
    }
}
