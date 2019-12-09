using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Unity.ProjectAuditor.Editor;
using UnityEngine;

namespace UnityEditor.ProjectAuditor.EditorTests
{
	class BoxingIssueTest {

		private ScriptResource m_ScriptResource;

		[SetUp]
		public void SetUp()
		{
			m_ScriptResource = new ScriptResource("MyClass.cs", "using UnityEngine; class MyClass : MonoBehaviour { void Start() { Debug.Log(\"The number of the beast is: \" + 666); } }");
		}

		[TearDown]
		public void TearDown()
		{
			m_ScriptResource.Delete();
		}

		[Test]
		public void AnalysisTestPasses()
		{
			var issues = ScriptIssueTestHelper.AnalyzeAndFindScriptIssues(m_ScriptResource.relativePath);
			
			Assert.AreEqual(1, issues.Count());
			
			var myIssue = issues.FirstOrDefault();
			
			Assert.NotNull(myIssue);
			Assert.NotNull(myIssue.descriptor);
			
			Assert.AreEqual(Rule.Action.Default, myIssue.descriptor.action);
			Assert.AreEqual(102000, myIssue.descriptor.id);
			Assert.True(string.IsNullOrEmpty(myIssue.descriptor.type));
			Assert.True(string.IsNullOrEmpty(myIssue.descriptor.method));
			
			Assert.True(myIssue.name.Equals("MyClass.Start"));
			Assert.True(myIssue.filename.Equals(m_ScriptResource.scriptName));
			Assert.True(myIssue.description.Equals("Conversion from value type 'Int32' to ref type"));
			Assert.True(myIssue.callingMethod.Equals("System.Void MyClass::Start()"));
			Assert.AreEqual(1, myIssue.line);
			Assert.AreEqual(IssueCategory.ApiCalls, myIssue.category);
		}
	}	
}

