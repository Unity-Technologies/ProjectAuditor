using System.Linq;
using NUnit.Framework;
using Unity.ProjectAuditor.Editor;

namespace UnityEditor.ProjectAuditor.EditorTests
{
	class EmptyMethodTest
	{
		private ScriptResource m_ScriptResource;
		
		[SetUp]
		public void SetUp()
		{
			 m_ScriptResource = new ScriptResource("MyClass.cs", "using UnityEngine; class MyClass : MonoBehaviour { void Update() { } }");
		}

		[TearDown]
		public void TearDown()
		{
			m_ScriptResource.Delete();
		}

		[Test]
		public void EmptyMethodIsFound()
		{
			var scriptIssues = ScriptIssueTestHelper.AnalyzeAndFindScriptIssues(m_ScriptResource.relativePath);
			
			Assert.AreEqual(1, scriptIssues.Count());

			var issue = scriptIssues.FirstOrDefault();
			
			Assert.NotNull(issue);
			Assert.NotNull(issue.descriptor);
			
			Assert.AreEqual(Rule.Action.Default, issue.descriptor.action);
			Assert.AreEqual(EmptyMethodAnalyzer.GetDescriptor().id, issue.descriptor.id);
			Assert.True(string.IsNullOrEmpty(issue.descriptor.type));
			Assert.True(string.IsNullOrEmpty(issue.descriptor.method));
			
			Assert.True(issue.name.Equals("MyClass.Update"));
			Assert.True(issue.filename.Equals(m_ScriptResource.scriptName));
			Assert.True(issue.description.Equals(EmptyMethodAnalyzer.GetDescriptor().description));
			Assert.True(issue.callingMethod.Equals("System.Void MyClass::Update()"));
			Assert.AreEqual(1, issue.line);
			Assert.AreEqual(IssueCategory.ApiCalls, issue.category);
		}
	}	
}

