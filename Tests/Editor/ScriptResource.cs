using System.IO;
using NUnit.Framework;

namespace UnityEditor.ProjectAuditor.EditorTests
{
    public class ScriptResource
    {
        const string TempFolder = "ProjectAuditor-Temp";
        private string m_ScriptName;

        public string relativePath
        {
            get { return Path.Combine("Assets", TempFolder, m_ScriptName);  }
        }

        public string scriptName
        {
            get { return m_ScriptName;  }
        }

        public ScriptResource(string scriptName, string content)
        {
            m_ScriptName = scriptName;            
            Directory.CreateDirectory(Path.GetDirectoryName(relativePath));

            File.WriteAllText(relativePath, content);

            Assert.True(File.Exists(relativePath));
			
            AssetDatabase.ImportAsset(relativePath, ImportAssetOptions.ForceUpdate);
        }

        public void Delete()
        {
            AssetDatabase.DeleteAsset(relativePath);
            Directory.Delete(Path.GetDirectoryName(relativePath), true);            
        }
    }
}