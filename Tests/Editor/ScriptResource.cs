using System.IO;
using NUnit.Framework;

namespace UnityEditor.ProjectAuditor.EditorTests
{
    public class ScriptResource
    {
        const string TempFolder = "ProjectAuditor-Temp";
        private string m_RelativePath;

        public string relativePath
        {
            get { return m_RelativePath;  }
        }

        public string scriptName
        {
            get { return Path.GetFileName(m_RelativePath);  }
        }

        public ScriptResource(string scriptName, string content)
        {
            m_RelativePath = Path.Combine("Assets", TempFolder, scriptName).Replace("\\", "/");        
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