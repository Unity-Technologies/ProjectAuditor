using System;
using System.Linq;
using NUnit.Framework;
using Unity.ProjectAuditor.Editor;
using Unity.ProjectAuditor.Editor.AssemblyUtils;
using Unity.ProjectAuditor.Editor.Core;
using Unity.ProjectAuditor.Editor.Diagnostic;
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
        public void SetUp()
        {
            m_MonoBehaviourWithEmptyEventMethod = new TestAsset("MonoBehaviourWithEmptyEventMethod.cs",
                "using UnityEngine; class MyBaseClass : MonoBehaviour { } class MonoBehaviourWithEmptyEventMethod : MyBaseClass { void Update() { ; } }"); // ';' should introduce a noop
            m_MonoBehaviourWithEmptyMethod = new TestAsset("MonoBehaviourWithEmptyMethod.cs",
                "using UnityEngine; class MonoBehaviourWithEmptyMethod : MonoBehaviour{ void NotAnEvent() { } }");
            m_NotMonoBehaviourWithEmptyMethod = new TestAsset("NotMonoBehaviourWithEmptyMethod.cs",
                "class NotMonoBehaviourWithEmptyMethod { void Update() { } }");
        }

        [Test]
        [TestCase(CodeOptimization.Debug)]
        [TestCase(CodeOptimization.Release)]
        public void CodeAnalysis_MonoBehaviourWithEmptyEventMethod_IsReported(CodeOptimization codeOptimization)
        {
            var prevCodeOptimization = m_CodeOptimization;
            m_CodeOptimization = codeOptimization;

            var scriptIssues = AnalyzeAndFindAssetIssues(m_MonoBehaviourWithEmptyEventMethod);

            m_CodeOptimization = prevCodeOptimization; // restore previous value

            Assert.AreEqual(1, scriptIssues.Count());

            var issue = scriptIssues.FirstOrDefault();

            Assert.NotNull(issue);
            var descriptor = DescriptorLibrary.GetDescriptor(issue.id);

            Assert.AreEqual(Severity.Moderate, descriptor.defaultSeverity);
            Assert.AreEqual(EmptyMethodAnalyzer.GetDescriptorID(), issue.id);
            Assert.True(string.IsNullOrEmpty(descriptor.type));
            Assert.True(string.IsNullOrEmpty(descriptor.method));

            Assert.AreEqual(m_MonoBehaviourWithEmptyEventMethod.fileName, issue.filename);
            Assert.AreEqual("MonoBehaviour method 'Update' is empty", issue.description);
            Assert.AreEqual("System.Void MonoBehaviourWithEmptyEventMethod::Update()", issue.GetContext());
            Assert.AreEqual(1, issue.line);
            Assert.AreEqual(IssueCategory.Code, issue.category);
        }

        [Test]
        public void CodeAnalysis_MonoBehaviourWithEmptyMethod_IsNotReported()
        {
            var scriptIssues = AnalyzeAndFindAssetIssues(m_MonoBehaviourWithEmptyMethod);

            Assert.AreEqual(0, scriptIssues.Count());
        }

        [Test]
        public void CodeAnalysis_NotMonoBehaviourWithEmptyMethod_IsNotReported()
        {
            var scriptIssues = AnalyzeAndFindAssetIssues(m_NotMonoBehaviourWithEmptyMethod);

            Assert.AreEqual(0, scriptIssues.Count());
        }
    }
}
