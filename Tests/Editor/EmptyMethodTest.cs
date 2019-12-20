using System.Linq;
using NUnit.Framework;
using Unity.ProjectAuditor.Editor;

namespace UnityEditor.ProjectAuditor.EditorTests
{
	class EmptyMethodTest
	{
		private ScriptResource m_ScriptResource;
		private ScriptResource m_ScriptResourceSimpleClass;
		
		[SetUp]
		public void SetUp()
		{
			 m_ScriptResource = new ScriptResource("MyMonoBehaviour.cs", "using UnityEngine; class MyBaseClass : MonoBehaviour { } class MyMonoBehaviour : MyBaseClass { void Update() { } }");
			 m_ScriptResourceSimpleClass = new ScriptResource("MyClass.cs", "class MyClass { void Update() { } }");
		}

		[TearDown]
		public void TearDown()
		{
			m_ScriptResource.Delete();
			m_ScriptResourceSimpleClass.Delete();
		}

		[Test]
		public void EmptyMonoBehaviourMethodIsFound()
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
			
			Assert.True(issue.name.Equals("System.Void MyMonoBehaviour::Update()"));
			Assert.True(issue.filename.Equals(m_ScriptResource.scriptName));
			Assert.True(issue.description.Equals("System.Void MyMonoBehaviour::Update()"));
			Assert.True(issue.callingMethod.Equals("System.Void MyMonoBehaviour::Update()"));
			Assert.AreEqual(1, issue.line);
			Assert.AreEqual(IssueCategory.ApiCalls, issue.category);
		}
		
		[Test]
		public void EmptyMethodIsNotFound()
		{
			var scriptIssues = ScriptIssueTestHelper.AnalyzeAndFindScriptIssues(m_ScriptResourceSimpleClass.relativePath);
			
			Assert.AreEqual(0, scriptIssues.Count());
		}
	}	
}

