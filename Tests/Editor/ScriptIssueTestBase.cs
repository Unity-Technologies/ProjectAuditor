using System.IO;
using NUnit.Framework;

namespace UnityEditor.ProjectAuditor.EditorTests
{
    public abstract class ScriptIssueTestBase
    {
        const string TempFolder = "ProjectAuditor-Temp";
        protected const string m_ScriptName = "MyScript.cs";

        protected string relativePath
        {
            get { return Path.Combine("Assets", TempFolder, m_ScriptName);  }
        }

        protected void CreateScript(string script)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(relativePath));

            var className = Path.GetFileNameWithoutExtension(m_ScriptName);
            File.WriteAllText(relativePath, script);

            Assert.True(File.Exists(relativePath));
			
            AssetDatabase.ImportAsset(relativePath, ImportAssetOptions.ForceUpdate);
        }

        protected void DeleteScript()
        {
            AssetDatabase.DeleteAsset(relativePath);
            Directory.Delete(Path.GetDirectoryName(relativePath), true);            
        }
    }
}