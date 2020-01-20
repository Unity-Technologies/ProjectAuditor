using System;
using System.IO;
using NUnit.Framework;
using UnityEngine;

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
            if (!File.Exists(relativePath))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(relativePath));   
            }

            File.WriteAllText(relativePath, content);

            Assert.True(File.Exists(relativePath));
			
            AssetDatabase.ImportAsset(relativePath, ImportAssetOptions.ForceUpdate);
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