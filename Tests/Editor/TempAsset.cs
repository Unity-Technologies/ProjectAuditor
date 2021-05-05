using System;
using System.IO;
using NUnit.Framework;

namespace UnityEditor.ProjectAuditor.EditorTests
{
    public class TempAsset
    {
        const string k_TempFolder = "ProjectAuditor-Temp";

        public readonly string relativePath;

        public string fileName
        {
            get { return Path.GetFileName(relativePath); }
        }

        private TempAsset(string fileName)
        {
            relativePath = Path.Combine("Assets", Path.Combine(k_TempFolder, fileName)).Replace("\\", "/");

            if (!File.Exists(relativePath))
                Directory.CreateDirectory(Path.GetDirectoryName(relativePath));
        }

        public TempAsset(string fileName, string content) :
            this(fileName)
        {
            File.WriteAllText(relativePath, content);

            Assert.True(File.Exists(relativePath));

            AssetDatabase.ImportAsset(relativePath, ImportAssetOptions.ForceUpdate);
        }

        public static TempAsset Save(UnityEngine.Object asset, string fileName)
        {
            var tempAsset = new TempAsset(fileName);
            AssetDatabase.CreateAsset(asset, tempAsset.relativePath);
            AssetDatabase.ImportAsset(tempAsset.relativePath, ImportAssetOptions.ForceUpdate);

            return tempAsset;
        }

        public static void Cleanup()
        {
            var path = Path.Combine("Assets", k_TempFolder);
            Directory.Delete(path, true);
            File.Delete(path + ".meta");
            AssetDatabase.Refresh();
        }
    }
}
