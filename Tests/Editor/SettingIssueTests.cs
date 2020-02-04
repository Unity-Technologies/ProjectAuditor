using System.Linq;
using NUnit.Framework;
using Unity.ProjectAuditor.Editor;

namespace UnityEditor.ProjectAuditor.EditorTests
{
	class SettingIssueTests
	{	
		[Test]
		public void SettingIssuesAreReported()
		{
			var projectAuditor = new Unity.ProjectAuditor.Editor.ProjectAuditor();

			// disabling stripEngineCode will be reported as an issue	
			PlayerSettings.stripEngineCode = false;
			
			// 0.02f is the default Time.fixedDeltaTime value and will be reported as an issue			
			UnityEngine.Time.fixedDeltaTime = 0.02f;
			
			var projectReport = projectAuditor.Audit();
			var issues = projectReport.GetIssues(IssueCategory.ProjectSettings);

			var fixedDeltaTimeIssue = issues.FirstOrDefault(i => i.descriptor.method.Equals("fixedDeltaTime"));
			Assert.NotNull(fixedDeltaTimeIssue);
			Assert.True(fixedDeltaTimeIssue.location.path.Equals("Project/Time"));
			
			UnityEngine.Time.fixedDeltaTime = 0.021f;
			
			projectReport = projectAuditor.Audit();
			issues = projectReport.GetIssues(IssueCategory.ProjectSettings);			
			Assert.Null(issues.FirstOrDefault(i => i.descriptor.method.Equals("fixedDeltaTime")));

			var playerSettingIssue =
				issues.FirstOrDefault(i => i.descriptor.method.Equals("stripEngineCode")); 
			Assert.NotNull(playerSettingIssue); 
			Assert.True(playerSettingIssue.location.path.Equals("Project/Player"));
		}
	}	
}

