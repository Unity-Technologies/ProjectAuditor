using System.Linq;
using NUnit.Framework;
using Unity.ProjectAuditor.Editor;

namespace UnityEditor.ProjectAuditor.EditorTests
{
	class ProjectAuditorTest {
		
		[Test]
		public void AnalysisTestPasses()
		{
			var projectReport = new ProjectReport();
			var projectAuditor = new Unity.ProjectAuditor.Editor.ProjectAuditor();

			UnityEngine.Time.fixedDeltaTime = 0.02f; // default value
			projectAuditor.Audit(projectReport);
			var issues = projectReport.GetIssues(IssueCategory.ProjectSettings);

			Assert.AreNotEqual(null, issues.FirstOrDefault(i => i.descriptor.method.Equals("fixedDeltaTime")));
			
			UnityEngine.Time.fixedDeltaTime = 0.021f;
			
			projectReport = new ProjectReport();
			projectAuditor.Audit(projectReport);
			issues = projectReport.GetIssues(IssueCategory.ProjectSettings);
			
			Assert.AreEqual(null, issues.FirstOrDefault(i => i.descriptor.method.Equals("fixedDeltaTime")));
		}
	}	
}

