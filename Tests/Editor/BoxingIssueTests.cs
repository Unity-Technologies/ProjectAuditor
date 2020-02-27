using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Unity.ProjectAuditor.Editor;
using UnityEngine;

namespace UnityEditor.ProjectAuditor.EditorTests
{
	class BoxingIssueTests 
	{
		private ScriptResource m_ScriptResourceBoxingInt;
		private ScriptResource m_ScriptResourceBoxingFloat;
		private ScriptResource m_ScriptResourceBoxingGenericRefType;
		private ScriptResource m_ScriptResourceBoxingGeneric;

		[OneTimeSetUp]
		public void SetUp()
		{
			m_ScriptResourceBoxingInt = new ScriptResource("BoxingIntTest.cs", "using System; class BoxingIntTest { Object Dummy() { return 666; } }");
			m_ScriptResourceBoxingFloat = new ScriptResource("BoxingFloatTest.cs", "using System; class BoxingFloatTest { Object Dummy() { return 666.0f; } }");
			m_ScriptResourceBoxingGenericRefType = new ScriptResource("BoxingGenericRefType.cs", "class SomeClass {}; class BoxingGenericRefType<T> where T : SomeClass { T refToGenericType; void Dummy() { if (refToGenericType == null){} } }");
			m_ScriptResourceBoxingGeneric = new ScriptResource("BoxingGeneric.cs", "class BoxingGeneric<T> { T refToGenericType; void Dummy() { if (refToGenericType == null){} } }");
		}

		[OneTimeTearDown]
		public void TearDown()
		{
			m_ScriptResourceBoxingInt.Delete();
			m_ScriptResourceBoxingFloat.Delete();
			m_ScriptResourceBoxingGenericRefType.Delete();
			m_ScriptResourceBoxingGeneric.Delete();
		}

		[Test]
		public void BoxingIntValueIsReported()
		{
			var issues = ScriptIssueTestHelper.AnalyzeAndFindScriptIssues(m_ScriptResourceBoxingInt);

			Assert.AreEqual(1, issues.Count());

			var boxingInt = issues.FirstOrDefault();

			// check issue
			Assert.NotNull(boxingInt);
			Assert.True(boxingInt.name.Equals("BoxingIntTest.Dummy"));
			Assert.True(boxingInt.filename.Equals(m_ScriptResourceBoxingInt.scriptName));
			Assert.True(boxingInt.description.Equals("Conversion from value type 'Int32' to ref type"));
			Assert.True(boxingInt.callingMethod.Equals("System.Object BoxingIntTest::Dummy()"));
			Assert.AreEqual(1, boxingInt.line);
			Assert.AreEqual(IssueCategory.ApiCalls, boxingInt.category);

			// check descriptor
			Assert.NotNull(boxingInt.descriptor);
			Assert.AreEqual(Rule.Action.Default, boxingInt.descriptor.action);
			Assert.AreEqual(102000, boxingInt.descriptor.id);
			Assert.True(string.IsNullOrEmpty(boxingInt.descriptor.type));
			Assert.True(string.IsNullOrEmpty(boxingInt.descriptor.method));
			Assert.False(string.IsNullOrEmpty(boxingInt.descriptor.description));
			Assert.True(boxingInt.descriptor.description.Equals("Boxing Allocation"));

		}

		[Test]
		public void BoxingFloatValueIsReported()
		{
			var issues = ScriptIssueTestHelper.AnalyzeAndFindScriptIssues(m_ScriptResourceBoxingFloat);
			
			Assert.AreEqual(1, issues.Count());
			
			var boxingFloat = issues.FirstOrDefault();

			// check issue
			Assert.NotNull(boxingFloat);
			Assert.True(boxingFloat.name.Equals("BoxingFloatTest.Dummy"));
			Assert.True(boxingFloat.filename.Equals(m_ScriptResourceBoxingFloat.scriptName));
			Assert.True(boxingFloat.description.Equals("Conversion from value type 'float' to ref type"));
			Assert.True(boxingFloat.callingMethod.Equals("System.Object BoxingFloatTest::Dummy()"));
			Assert.AreEqual(1, boxingFloat.line);
			Assert.AreEqual(IssueCategory.ApiCalls, boxingFloat.category);
			
			// check descriptor
			Assert.NotNull(boxingFloat.descriptor);
			Assert.AreEqual(Rule.Action.Default, boxingFloat.descriptor.action);
			Assert.AreEqual(102000, boxingFloat.descriptor.id);
			Assert.True(string.IsNullOrEmpty(boxingFloat.descriptor.type));
			Assert.True(string.IsNullOrEmpty(boxingFloat.descriptor.method));
			Assert.False(string.IsNullOrEmpty(boxingFloat.descriptor.description));
			Assert.True(boxingFloat.descriptor.description.Equals("Boxing Allocation"));
		}

		[Test]
		public void BoxingGenericIsReported()
		{
			var issues = ScriptIssueTestHelper.AnalyzeAndFindScriptIssues(m_ScriptResourceBoxingGeneric);

			Assert.AreEqual(1, issues.Count());
		}
		
		[Test]
		public void BoxingGenericRefTypeIsNotReported()
		{
			var issues = ScriptIssueTestHelper.AnalyzeAndFindScriptIssues(m_ScriptResourceBoxingGenericRefType);

			Assert.Zero(issues.Count());			
		}

	}	
}
