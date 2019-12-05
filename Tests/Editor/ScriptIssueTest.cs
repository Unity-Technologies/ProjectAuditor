using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Unity.ProjectAuditor.Editor;
using UnityEngine;

namespace UnityEditor.ProjectAuditor.EditorTests
{
	class ScriptIssueTest {
			
		const string tempFolder = "ProjectAuditor-Temp";
		const string scriptName = "MyScript.cs";

		private string relativePath
		{
			get { return Path.Combine("Assets", tempFolder, scriptName);  }
		}
		
		[SetUp]
		public void SetUp()
		{
			Directory.CreateDirectory(Path.GetDirectoryName(relativePath));

			var className = Path.GetFileNameWithoutExtension(scriptName);
			File.WriteAllText(relativePath, string.Format("using UnityEngine; class {0} : MonoBehaviour {{ void Start() {{ Debug.Log(Camera.main.name); }} }}", className));

			Assert.True(File.Exists(relativePath));
			
			AssetDatabase.ImportAsset(relativePath, ImportAssetOptions.ForceUpdate);
		}

		[TearDown]
		public void TearDown()
		{
			AssetDatabase.DeleteAsset(relativePath);
			Directory.Delete(Path.GetDirectoryName(relativePath), true);
		}

		[Test]
		public void AnalysisTestPasses()
		{
			var projectAuditor = new Unity.ProjectAuditor.Editor.ProjectAuditor();

			var projectReport = projectAuditor.Audit();
			var issues = projectReport.GetIssues(IssueCategory.ApiCalls);

			Assert.NotNull(issues);
			
			Assert.Positive(issues.Count());

			issues = issues.Where(i => i.relativePath.Equals(relativePath));
			
			Assert.Positive(issues.Count());
			
			var myIssue = issues.FirstOrDefault();
			
			Assert.NotNull(myIssue);
			Assert.NotNull(myIssue.descriptor);
			
			Assert.AreEqual(Rule.Action.Default, myIssue.descriptor.action);
			Assert.AreEqual(101000, myIssue.descriptor.id);
			Assert.True(myIssue.descriptor.type.Equals("UnityEngine.Camera"));
			Assert.True(myIssue.descriptor.method.Equals("main"));
			
			Assert.True(myIssue.name.Equals("Camera.get_main"));
			Assert.True(myIssue.filename.Equals(scriptName));
			Assert.True(myIssue.description.Equals("UnityEngine.Camera.main"));
			Assert.True(myIssue.callingMethod.Equals("System.Void MyScript::Start()"));
			Assert.AreEqual(1, myIssue.line);
			Assert.AreEqual(IssueCategory.ApiCalls, myIssue.category);
		}
	}	
}

