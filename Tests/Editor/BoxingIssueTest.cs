using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Unity.ProjectAuditor.Editor;
using UnityEngine;

namespace UnityEditor.ProjectAuditor.EditorTests
{
	class BoxingIssueTest {

		private ScriptResource m_ScriptResourceBoxingInt;
		private ScriptResource m_ScriptResourceBoxingFloat;

		[SetUp]
		public void SetUp()
		{
			m_ScriptResourceBoxingInt = new ScriptResource("BoxingIntTest.cs", "using UnityEngine; class BoxingIntTest : MonoBehaviour { void Start() { Debug.Log(\"The number of the beast is: \" + 666); } }");
			m_ScriptResourceBoxingFloat = new ScriptResource("BoxingFloatTest.cs", "using UnityEngine; class BoxingFloatTest : MonoBehaviour { void Start() { Debug.Log(\"The number of the beast is: \" + 666.0f); } }");
		}

		[TearDown]
		public void TearDown()
		{
			m_ScriptResourceBoxingInt.Delete();
			m_ScriptResourceBoxingFloat.Delete();
		}

		[Test]
		public void AnalysisTestPasses()
		{
			var issues = ScriptIssueTestHelper.AnalyzeAndFindScriptIssues(m_ScriptResourceBoxingInt.relativePath);
			
			Assert.AreEqual(1, issues.Count());
			
			var boxingInt = issues.FirstOrDefault();
			
			// check issue
			Assert.NotNull(boxingInt);
			Assert.True(boxingInt.name.Equals("BoxingIntTest.Start"));
			Assert.True(boxingInt.filename.Equals(m_ScriptResourceBoxingInt.scriptName));
			Assert.True(boxingInt.description.Equals("Conversion from value type 'Int32' to ref type"));
			Assert.True(boxingInt.callingMethod.Equals("System.Void BoxingIntTest::Start()"));
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

						
			issues = ScriptIssueTestHelper.AnalyzeAndFindScriptIssues(m_ScriptResourceBoxingFloat.relativePath);
			
			Assert.AreEqual(1, issues.Count());
			
			var boxingFloat = issues.FirstOrDefault();

			// check issue
			Assert.NotNull(boxingFloat);
			Assert.True(boxingFloat.name.Equals("BoxingFloatTest.Start"));
			Assert.True(boxingFloat.filename.Equals(m_ScriptResourceBoxingFloat.scriptName));
			Assert.True(boxingFloat.description.Equals("Conversion from value type 'float' to ref type"));
			Assert.True(boxingFloat.callingMethod.Equals("System.Void BoxingFloatTest::Start()"));
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
	}	
}

