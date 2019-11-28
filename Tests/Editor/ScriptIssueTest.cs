using System.IO;
using System.Linq;
using NUnit.Framework;
using Unity.ProjectAuditor.Editor;

namespace UnityEditor.ProjectAuditor.EditorTests
{
	class ScriptIssueTest {
			
		const string scriptPath = "Assets/ProjectAuditorTemp";
		const string scriptName = "MyScript.cs";
		
		[SetUp]
		public void SetUp()
		{
			Directory.CreateDirectory(scriptPath);

			var className = Path.GetFileNameWithoutExtension(scriptName);
			File.WriteAllText(Path.Combine(scriptPath, scriptName), string.Format("using UnityEngine; class {0} : MonoBehaviour {{ void Start() {{ Debug.Log(Camera.main.name); }} }}", className));
			
			AssetDatabase.Refresh();
		}

//		[TearDown]
//		public void TearDown()
//		{
//			Directory.Delete(Path.Combine(scriptPath, scriptName));
//			Directory.Delete(scriptPath);
//		}

		[Test]
		public void AnalysisTestPasses()
		{
			var projectReport = new ProjectReport();
			var projectAuditor = new Unity.ProjectAuditor.Editor.ProjectAuditor();

			projectAuditor.Audit(projectReport);
			var issues = projectReport.GetIssues(IssueCategory.ApiCalls);

			var myIssue = issues.FirstOrDefault(i => i.filename.Equals(Path.Combine(scriptPath, scriptName)));
			
			Assert.NotNull(myIssue);
		}
	}	
}

