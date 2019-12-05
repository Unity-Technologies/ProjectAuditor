using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Unity.ProjectAuditor.Editor;
using UnityEngine;

namespace UnityEditor.ProjectAuditor.EditorTests
{
	class BoxingGenericIssueTest
	{
		private ScriptResource m_ScriptResource;

		[SetUp]
		public void SetUp()
		{
			m_ScriptResource = new ScriptResource("MyClass.cs", "using UnityEngine; class SomeClass {}; class MyClass<T> where T : SomeClass { T refToGenericType; void Start() { if (refToGenericType == null){} } }");
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
			
			Assert.Zero(issues.Count());
		}
	}	
}

