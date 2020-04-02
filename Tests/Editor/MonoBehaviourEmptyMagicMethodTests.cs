using System.Linq;
using NUnit.Framework;
using Unity.ProjectAuditor.Editor;
using Unity.ProjectAuditor.Editor.InstructionAnalyzers;

namespace UnityEditor.ProjectAuditor.EditorTests
{
    internal class MonoBehaviourEmptyMagicMethodTests
    {
        private ScriptResource m_MonoBehaviourWithEmptyMagicMethod;
        private ScriptResource m_MonoBehaviourWithEmptyMethod;
        private ScriptResource m_NotMonoBehaviourWithEmptyMethod;

        [OneTimeSetUp]
        public void SetUp()
        {
            m_MonoBehaviourWithEmptyMagicMethod = new ScriptResource("MonoBehaviourWithEmptyMagicMethod.cs",
                "using UnityEngine; class MyBaseClass : MonoBehaviour { } class MonoBehaviourWithEmptyMagicMethod : MyBaseClass { void Update() { } }");
            m_MonoBehaviourWithEmptyMethod = new ScriptResource("MonoBehaviourWithEmptyMethod.cs",
                "using UnityEngine; class MonoBehaviourWithEmptyMethod : MonoBehaviour{ void NotMagicMethod() { } }");
            m_NotMonoBehaviourWithEmptyMethod = new ScriptResource("NotMonoBehaviourWithEmptyMethod.cs",
                "class NotMonoBehaviourWithEmptyMethod { void Update() { } }");
        }

        [OneTimeTearDown]
        public void TearDown()
        {
            m_MonoBehaviourWithEmptyMagicMethod.Delete();
            m_MonoBehaviourWithEmptyMethod.Delete();
            m_NotMonoBehaviourWithEmptyMethod.Delete();
        }

        [Test]
        public void MonoBehaviourWithEmptyMagicMethodIsReported()
        {
            var scriptIssues = ScriptIssueTestHelper.AnalyzeAndFindScriptIssues(m_MonoBehaviourWithEmptyMagicMethod);

            Assert.AreEqual(1, scriptIssues.Count());

            var issue = scriptIssues.FirstOrDefault();

            Assert.NotNull(issue);
            Assert.NotNull(issue.descriptor);

            Assert.AreEqual(Rule.Action.Default, issue.descriptor.action);
            Assert.AreEqual(EmptyMethodAnalyzer.GetDescriptor().id, issue.descriptor.id);
            Assert.True(string.IsNullOrEmpty(issue.descriptor.type));
            Assert.True(string.IsNullOrEmpty(issue.descriptor.method));

            Assert.True(issue.name.Equals("MonoBehaviourWithEmptyMagicMethod.Update"));
            Assert.True(issue.filename.Equals(m_MonoBehaviourWithEmptyMagicMethod.scriptName));
            Assert.True(issue.description.Equals("System.Void MonoBehaviourWithEmptyMagicMethod::Update()"));
            Assert.True(issue.callingMethod.Equals("System.Void MonoBehaviourWithEmptyMagicMethod::Update()"));
            Assert.AreEqual(1, issue.line);
            Assert.AreEqual(IssueCategory.ApiCalls, issue.category);
        }

        [Test]
        public void MonoBehaviourWithEmptyMethodIsNotReported()
        {
            var scriptIssues = ScriptIssueTestHelper.AnalyzeAndFindScriptIssues(m_MonoBehaviourWithEmptyMethod);

            Assert.AreEqual(0, scriptIssues.Count());
        }

        [Test]
        public void NotMonoBehaviourWithEmptyMethodIsNotReported()
        {
            var scriptIssues = ScriptIssueTestHelper.AnalyzeAndFindScriptIssues(m_NotMonoBehaviourWithEmptyMethod);

            Assert.AreEqual(0, scriptIssues.Count());
        }
    }
}
