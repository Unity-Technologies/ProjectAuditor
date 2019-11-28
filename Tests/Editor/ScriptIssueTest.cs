using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Unity.ProjectAuditor.Editor;

namespace UnityEditor.ProjectAuditor.EditorTests
{
	class ScriptIssueTest {
			
		const string tempPath = "Assets/ProjectAuditor-Temp";
		const string scriptName = "MyScript.cs";
		
		[SetUp]
		public void SetUp()
		{
			Directory.CreateDirectory(tempPath);

			var relativePath = Path.Combine(tempPath, scriptName);
			var className = Path.GetFileNameWithoutExtension(scriptName);
			File.WriteAllText(relativePath, string.Format("using UnityEngine; class {0} : MonoBehaviour {{ void Start() {{ Debug.Log(Camera.main.name); }} }}", className));
			
			AssetDatabase.ImportAsset(relativePath, ImportAssetOptions.ForceUpdate);
		}

		[TearDown]
		public void TearDown()
		{
			Directory.Delete(tempPath, true);
		}

		[Test]
		public void AnalysisTestPasses()
		{
			var projectReport = new ProjectReport();
			var projectAuditor = new Unity.ProjectAuditor.Editor.ProjectAuditor();

			projectAuditor.Audit(projectReport);
			var issues = projectReport.GetIssues(IssueCategory.ApiCalls);

			Assert.NotNull(issues);
			
			Assert.Positive(issues.Count());

			issues = issues.Where(i => i.relativePath.Equals(Path.Combine(tempPath, scriptName)));
			
			Assert.AreEqual(1, issues.Count());
			
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

