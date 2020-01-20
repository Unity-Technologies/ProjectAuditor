using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Unity.ProjectAuditor.Editor;
using UnityEngine;

namespace UnityEditor.ProjectAuditor.EditorTests
{
	class ScriptIssueTest
	{
		private ScriptResource m_ScriptResource;
		
		[SetUp]
		public void SetUp()
		{
			 m_ScriptResource = new ScriptResource("MyClass.cs", "using UnityEngine; class MyClass : MonoBehaviour { void Start() { Debug.Log(Camera.main.name); } }");
		}

		[TearDown]
		public void TearDown()
		{
			m_ScriptResource.Delete();
		}

		[Test]
		public void AnalysisTestPasses()
		{
			var issues = ScriptIssueTestHelper.AnalyzeAndFindScriptIssues(m_ScriptResource);
			
			Assert.AreEqual(1, issues.Count());

			var myIssue = issues.FirstOrDefault();
			
			Assert.NotNull(myIssue);
			Assert.NotNull(myIssue.descriptor);
			
			Assert.AreEqual(Rule.Action.Default, myIssue.descriptor.action);
			Assert.AreEqual(101000, myIssue.descriptor.id);
			Assert.True(myIssue.descriptor.type.Equals("UnityEngine.Camera"));
			Assert.True(myIssue.descriptor.method.Equals("main"));
			
			Assert.True(myIssue.name.Equals("Camera.get_main"));
			Assert.True(myIssue.filename.Equals(m_ScriptResource.scriptName));
			Assert.True(myIssue.description.Equals("UnityEngine.Camera.main"));
			Assert.True(myIssue.callingMethod.Equals("System.Void MyClass::Start()"));
			Assert.AreEqual(1, myIssue.line);
			Assert.AreEqual(IssueCategory.ApiCalls, myIssue.category);
		}
	}	
}

