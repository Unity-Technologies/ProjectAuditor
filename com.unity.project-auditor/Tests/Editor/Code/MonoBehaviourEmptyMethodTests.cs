using System.Linq;
using NUnit.Framework;
using Unity.ProjectAuditor.Editor;
using Unity.ProjectAuditor.Editor.InstructionAnalyzers;
using Unity.ProjectAuditor.Editor.Tests.Common;

namespace Unity.ProjectAuditor.EditorTests
{
    class MonoBehaviourEmptyMethodTests : TestFixtureBase
    {
        TestAsset m_MonoBehaviourWithEmptyEventMethod;
        TestAsset m_MonoBehaviourWithEmptyMethod;
        TestAsset m_NotMonoBehaviourWithEmptyMethod;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            m_MonoBehaviourWithEmptyEventMethod = new TestAsset("MonoBehaviourWithEmptyEventMethod.cs",
                "using UnityEngine; class MyBaseClass : MonoBehaviour { } class MonoBehaviourWithEmptyEventMethod : MyBaseClass { void Update() { ; } }"); // ';' should introduce a noop
            m_MonoBehaviourWithEmptyMethod = new TestAsset("MonoBehaviourWithEmptyMethod.cs",
                "using UnityEngine; class MonoBehaviourWithEmptyMethod : MonoBehaviour{ void NotAnEvent() { } }");
            m_NotMonoBehaviourWithEmptyMethod = new TestAsset("NotMonoBehaviourWithEmptyMethod.cs",
                "class NotMonoBehaviourWithEmptyMethod { void Update() { } }");

            AnalyzeTestAssets();
        }

        [Test]
        [TestCase(CodeOptimization.Debug)]
        [TestCase(CodeOptimization.Release)]
        public void CodeAnalysis_MonoBehaviourWithEmptyEventMethod_IsReported(CodeOptimization codeOptimization)
        {
            var prevCodeOptimization = m_CodeOptimization;
            m_CodeOptimization = codeOptimization;

            var scriptIssues = GetIssuesForAsset(m_MonoBehaviourWithEmptyEventMethod);

            m_CodeOptimization = prevCodeOptimization; // restore previous value

            Assert.AreEqual(1, scriptIssues.Count());

            var issue = scriptIssues.FirstOrDefault();

            Assert.NotNull(issue);
            var descriptor = issue.Id.GetDescriptor();

            Assert.AreEqual(Severity.Moderate, descriptor.DefaultSeverity);
            Assert.AreEqual(EmptyMethodAnalyzer.GetDescriptorID(), issue.Id.ToString());
            Assert.True(string.IsNullOrEmpty(descriptor.Type));
            Assert.True(string.IsNullOrEmpty(descriptor.Method));

            Assert.AreEqual(m_MonoBehaviourWithEmptyEventMethod.FileName, issue.Filename);
            Assert.AreEqual("MonoBehaviour method 'Update' is empty", issue.Description);
            Assert.AreEqual("System.Void MonoBehaviourWithEmptyEventMethod::Update()", issue.GetContext());
            Assert.AreEqual(1, issue.Line);
            Assert.AreEqual(IssueCategory.Code, issue.Category);
        }

        [Test]
        public void CodeAnalysis_MonoBehaviourWithEmptyMethod_IsNotReported()
        {
            var scriptIssues = GetIssuesForAsset(m_MonoBehaviourWithEmptyMethod);

            Assert.AreEqual(0, scriptIssues.Count());
        }

        [Test]
        public void CodeAnalysis_NotMonoBehaviourWithEmptyMethod_IsNotReported()
        {
            var scriptIssues = GetIssuesForAsset(m_NotMonoBehaviourWithEmptyMethod);

            Assert.AreEqual(0, scriptIssues.Count());
        }
    }
}
