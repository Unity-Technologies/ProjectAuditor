using System.Linq;
using NUnit.Framework;

namespace UnityEditor.ProjectAuditor.EditorTests
{
    public class PerfCriticalContextTests
    {
        private ScriptResource m_ScriptResourceIssueInClassInheritedFromMonoBehaviour;
        private ScriptResource m_ScriptResourceIssueInClassMethodCalledFromMonoBehaviourUpdate;
        private ScriptResource m_ScriptResourceIssueInMonoBehaviourUpdate;
        private ScriptResource m_ScriptResourceIssueInSimpleClass;

        [OneTimeSetUp]
        public void SetUp()
        {
            m_ScriptResourceIssueInSimpleClass = new ScriptResource("IssueInSimpleClass.cs", @"
using UnityEngine;
class IssueInSimpleClass	
{
	void Dummy()
	{
		// Accessing Camera.main property is not recommended and will be reported as a possible performance problem.
		Debug.Log(Camera.main.name);
	}
}
");

            m_ScriptResourceIssueInMonoBehaviourUpdate = new ScriptResource("IssueInMonoBehaviourUpdate.cs", @"
using UnityEngine;
class IssueInMonoBehaviourUpdate : MonoBehaviour
{
	void Update()
	{
		Debug.Log(Camera.main.name);
	}
}
");

            m_ScriptResourceIssueInClassMethodCalledFromMonoBehaviourUpdate = new ScriptResource(
                "IssueInClassMethodCalledFromMonoBehaviourUpdate.cs", @"
using UnityEngine;

class IssueInClassMethodCalledFromMonoBehaviourUpdate : MonoBehaviour
{
	class NestedClass
	{
		public void Dummy()
		{
			// Accessing Camera.main property is not recommended and will be reported as a possible performance problem.
			Debug.Log(Camera.main.name);
		}
	}

	NestedClass m_MyObj;
	void Update()
	{
		m_MyObj.Dummy();
	}
}
");

            m_ScriptResourceIssueInClassInheritedFromMonoBehaviour = new ScriptResource(
                "IssueInClassInheritedFromMonoBehaviour.cs", @"
using UnityEngine;
class A : MonoBehaviour
{
}

class B : A
{
	void Update()
	{
		Debug.Log(Camera.main.name);
	}
}
");
        }

        [OneTimeTearDown]
        public void TearDown()
        {
            m_ScriptResourceIssueInSimpleClass.Delete();
            m_ScriptResourceIssueInMonoBehaviourUpdate.Delete();
            m_ScriptResourceIssueInClassMethodCalledFromMonoBehaviourUpdate.Delete();
            m_ScriptResourceIssueInClassInheritedFromMonoBehaviour.Delete();
        }

        [Test]
        public void IssueInSimpleClass()
        {
            var issues = ScriptIssueTestHelper.AnalyzeAndFindScriptIssues(m_ScriptResourceIssueInSimpleClass);
            var issue = issues.First();
            Assert.False(issue.isPerfCriticalContext);
        }

        [Test]
        public void IssueInMonoBehaviourUpdate()
        {
            var issues = ScriptIssueTestHelper.AnalyzeAndFindScriptIssues(m_ScriptResourceIssueInMonoBehaviourUpdate);
            var issue = issues.First();
            Assert.True(issue.isPerfCriticalContext);
        }

        [Test]
        public void IssueInClassMethodCalledFromMonoBehaviourUpdate()
        {
            var issues =
                ScriptIssueTestHelper.AnalyzeAndFindScriptIssues(
                    m_ScriptResourceIssueInClassMethodCalledFromMonoBehaviourUpdate);
            var issue = issues.First();
            Assert.True(issue.isPerfCriticalContext);
        }

        [Test]
        public void IssueInClassInheritedFromMonoBehaviour()
        {
            var issues =
                ScriptIssueTestHelper.AnalyzeAndFindScriptIssues(
                    m_ScriptResourceIssueInClassInheritedFromMonoBehaviour);
            var issue = issues.First();
            Assert.True(issue.isPerfCriticalContext);
        }
    }
}