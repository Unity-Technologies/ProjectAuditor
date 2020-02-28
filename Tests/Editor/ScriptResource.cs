using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace UnityEditor.ProjectAuditor.EditorTests
{
    public class ScriptResource
    {
        private const string TempFolder = "ProjectAuditor-Temp";

        public ScriptResource(string scriptName, string content)
        {
            relativePath = Path.Combine("Assets", Path.Combine(TempFolder, scriptName)).Replace("\\", "/");
            if (!File.Exists(relativePath)) Directory.CreateDirectory(Path.GetDirectoryName(relativePath));

            File.WriteAllText(relativePath, content);

            Assert.True(File.Exists(relativePath));

            AssetDatabase.ImportAsset(relativePath, ImportAssetOptions.ForceUpdate);
        }

        public string relativePath { get; set; }

        public string scriptName
        {
            get { return Path.GetFileName(relativePath); }
        }

        public void Delete()
        {
            AssetDatabase.DeleteAsset(relativePath);
            try
            {
                Directory.Delete(Path.GetDirectoryName(relativePath));
            }
            catch (IOException e)
            {
                // there might be a script still.
                Debug.LogWarning(e);
            }
        }
    }
}