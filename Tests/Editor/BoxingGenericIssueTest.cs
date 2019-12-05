using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Unity.ProjectAuditor.Editor;
using UnityEngine;

namespace UnityEditor.ProjectAuditor.EditorTests
{
	class BoxingGenericIssueTest : ScriptIssueTestBase{
			
		[SetUp]
		public void SetUp()
		{
			CreateScript("using UnityEngine; class SomeClass {}; class MyClass<T> where T : SomeClass { T refToGenericType; void Start() { if (refToGenericType == null){} } }");
		}

		[TearDown]
		public void TearDown()
		{
			DeleteScript();
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

			issues = issues.Where(i => i.relativePath.Equals(relativePath));
			
			Assert.Zero(issues.Count());
		}
	}	
}

