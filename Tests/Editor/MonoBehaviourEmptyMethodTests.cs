using System;
using System.Linq;
using NUnit.Framework;
using Unity.ProjectAuditor.Editor;
using Unity.ProjectAuditor.Editor.CodeAnalysis;
using Unity.ProjectAuditor.Editor.InstructionAnalyzers;
using Unity.ProjectAuditor.Editor.Utils;

namespace UnityEditor.ProjectAuditor.EditorTests
{
    class MonoBehaviourEmptyMethodTests
    {
        TempAsset m_MonoBehaviourWithEmptyEventMethod;
        TempAsset m_MonoBehaviourWithEmptyMethod;
        TempAsset m_NotMonoBehaviourWithEmptyMethod;

        [OneTimeSetUp]
        public void SetUp()
        {
            m_MonoBehaviourWithEmptyEventMethod = new TempAsset("MonoBehaviourWithEmptyEventMethod.cs",
                "using UnityEngine; class MyBaseClass : MonoBehaviour { } class MonoBehaviourWithEmptyEventMethod : MyBaseClass { void Update() { ; } }"); // ';' should introduce a noop
            m_MonoBehaviourWithEmptyMethod = new TempAsset("MonoBehaviourWithEmptyMethod.cs",
                "using UnityEngine; class MonoBehaviourWithEmptyMethod : MonoBehaviour{ void NotAnEvent() { } }");
            m_NotMonoBehaviourWithEmptyMethod = new TempAsset("NotMonoBehaviourWithEmptyMethod.cs",
                "class NotMonoBehaviourWithEmptyMethod { void Update() { } }");
        }

        [OneTimeTearDown]
        public void TearDown()
        {
            TempAsset.Cleanup();
        }

        [Test]
        [TestCase(CodeOptimization.Debug)]
        [TestCase(CodeOptimization.Release)]
        public void MonoBehaviourWithEmptyEventMethodIsReported(CodeOptimization codeOptimization)
        {
            var prevCodeOptimization = AssemblyCompilationPipeline.CodeOptimization;
            AssemblyCompilationPipeline.CodeOptimization = codeOptimization;

            var scriptIssues = Utility.AnalyzeAndFindAssetIssues(m_MonoBehaviourWithEmptyEventMethod);

            AssemblyCompilationPipeline.CodeOptimization = prevCodeOptimization; // restore previous value

            Assert.AreEqual(1, scriptIssues.Count());

            var issue = scriptIssues.FirstOrDefault();

            Assert.NotNull(issue);
            Assert.NotNull(issue.descriptor);

            Assert.AreEqual(Rule.Severity.Default, issue.descriptor.severity);
            Assert.AreEqual(EmptyMethodAnalyzer.GetDescriptor().id, issue.descriptor.id);
            Assert.True(string.IsNullOrEmpty(issue.descriptor.type));
            Assert.True(string.IsNullOrEmpty(issue.descriptor.method));

            Assert.True(issue.name.Equals("MonoBehaviourWithEmptyEventMethod.Update"));
            Assert.True(issue.filename.Equals(m_MonoBehaviourWithEmptyEventMethod.fileName));
            Assert.True(issue.description.Equals("System.Void MonoBehaviourWithEmptyEventMethod::Update()"));
            Assert.True(issue.GetCallingMethod().Equals("System.Void MonoBehaviourWithEmptyEventMethod::Update()"));
            Assert.AreEqual(1, issue.line);
            Assert.AreEqual(IssueCategory.Code, issue.category);
        }

        [Test]
        public void MonoBehaviourWithEmptyMethodIsNotReported()
        {
            var scriptIssues = Utility.AnalyzeAndFindAssetIssues(m_MonoBehaviourWithEmptyMethod);

            Assert.AreEqual(0, scriptIssues.Count());
        }

        [Test]
        public void NotMonoBehaviourWithEmptyMethodIsNotReported()
        {
            var scriptIssues = Utility.AnalyzeAndFindAssetIssues(m_NotMonoBehaviourWithEmptyMethod);

            Assert.AreEqual(0, scriptIssues.Count());
        }
    }
}
