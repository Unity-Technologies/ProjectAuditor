using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Unity.ProjectAuditor.Editor;
using UnityEngine;

namespace UnityEditor.ProjectAuditor.EditorTests
{
	class BoxingIssueTest : ScriptIssueTestBase{
			
		[SetUp]
		public void SetUp()
		{
			CreateScript("using UnityEngine; class MyClass : MonoBehaviour { void Start() { Debug.Log(\"The number of the beast is: \" + 666); } }");
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
			
			Assert.Positive(issues.Count());
			
			var myIssue = issues.FirstOrDefault();
			
			Assert.NotNull(myIssue);
			Assert.NotNull(myIssue.descriptor);
			
			Assert.AreEqual(Rule.Action.Default, myIssue.descriptor.action);
			Assert.AreEqual(102000, myIssue.descriptor.id);
			Assert.True(string.IsNullOrEmpty(myIssue.descriptor.type));
			Assert.True(string.IsNullOrEmpty(myIssue.descriptor.method));
			
			Assert.True(myIssue.name.Equals("MyClass.Start"));
			Assert.True(myIssue.filename.Equals(m_ScriptName));
			Assert.True(myIssue.description.Equals("Box"));
			Assert.True(myIssue.callingMethod.Equals("System.Void MyClass::Start()"));
			Assert.AreEqual(1, myIssue.line);
			Assert.AreEqual(IssueCategory.ApiCalls, myIssue.category);
		}
	}	
}

