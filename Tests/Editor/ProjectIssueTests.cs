using System;
using System.Collections;
using NUnit.Framework;
using Unity.ProjectAuditor.Editor;
using Unity.ProjectAuditor.Editor.CodeAnalysis;
using Unity.ProjectAuditor.Editor.Core;
using Unity.ProjectAuditor.Editor.Diagnostic;
using Unity.ProjectAuditor.Editor.Utils;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace Unity.ProjectAuditor.EditorTests
{
    [Serializable]
    class ProjectIssueTests
    {
        readonly Descriptor m_Descriptor = new Descriptor
            (
            "TDD2001",
            "test",
            Area.CPU,
            "this is not actually a problem",
            "do nothing"
            );

        Descriptor m_CriticalIssueDescriptor = new Descriptor
            (
            "TDD2002",
            "test",
            Area.CPU,
            "this is not actually a problem",
            "do nothing"
            )
        {
            defaultSeverity = Severity.Critical
        };

        [SerializeField]
        ProjectIssue m_Issue;

        [OneTimeSetUp]
        public void SetUp()
        {
            DescriptorLibrary.RegisterDescriptor(m_Descriptor.id, m_Descriptor);
            DescriptorLibrary.RegisterDescriptor(m_CriticalIssueDescriptor.id, m_CriticalIssueDescriptor);
        }

        [Test]
        public void ProjectIssue_NewIssue_IsInitialized()
        {
            var description = "dummy issue";
            var diagnostic = new ProjectIssue(IssueCategory.Code, m_Descriptor.id, description);
            Assert.AreEqual(string.Empty, diagnostic.filename);
            Assert.AreEqual(string.Empty, diagnostic.relativePath);
            Assert.AreEqual(string.Empty, diagnostic.GetContext());
            Assert.AreEqual(description, diagnostic.description);
            Assert.AreNotEqual(Severity.Critical, diagnostic.severity);

            Assert.IsFalse(diagnostic.IsMajorOrCritical());
        }

        [Test]
        public void ProjectIssue_NewIssue_IsCritical()
        {
            var description = "dummy issue";
            var diagnostic = new ProjectIssue(IssueCategory.Code, m_CriticalIssueDescriptor.id, description);
            Assert.AreEqual(string.Empty, diagnostic.filename);
            Assert.AreEqual(string.Empty, diagnostic.relativePath);
            Assert.AreEqual(string.Empty, diagnostic.GetContext());
            Assert.AreEqual(description, diagnostic.description);

            // the issue should be critical as per the descriptor
            Assert.AreEqual(Severity.Critical, diagnostic.severity);

            Assert.IsTrue(diagnostic.IsMajorOrCritical());
        }

        [UnityTest]
        public IEnumerator ProjectIssue_Priority_PersistsAfterDomainReload()
        {
            var description = "dummy issue";
            m_Issue = new ProjectIssue(IssueCategory.Code, m_Descriptor.id, description);
            m_Issue.severity = Severity.Major;

#if UNITY_2019_3_OR_NEWER
            EditorUtility.RequestScriptReload();
            yield return new WaitForDomainReload();

            Assert.AreEqual(Severity.Major, m_Issue.severity);
#else
            yield return null;
#endif
        }

        [Test]
        public void ProjectIssue_CustomProperties_AreSet()
        {
            object[] properties =
            {
                "property #0",
                "property #1"
            };
            var context = new AnalysisContext();
            var issue = (ProjectIssue)context.Create(IssueCategory.Code, m_Descriptor.id, "dummy issue")
                .WithCustomProperties(properties);

            Assert.AreEqual(2, issue.GetNumCustomProperties());
            Assert.AreEqual(properties[0], issue.GetCustomProperty(0));
            Assert.AreEqual(properties[1], issue.GetCustomProperty(1));
        }

        [Test]
        public void ProjectIssue_CustomProperties_AreNotSet()
        {
            var issue = new ProjectIssue(IssueCategory.Code, m_Descriptor.id, "dummy issue");

            Assert.AreEqual(0, issue.GetNumCustomProperties());
        }

        [Test]
        public void ProjectIssue_Properties_AreSet()
        {
            object[] properties =
            {
                "property #0",
                "property #1"
            };
            var context = new AnalysisContext();
            var issue = (ProjectIssue)context.Create(IssueCategory.Code, m_Descriptor.id, "dummy issue")
                .WithCustomProperties(properties)
                .WithLocation("Assets/Dummy.cs");

            Assert.AreEqual(2, issue.GetNumCustomProperties());
            Assert.AreEqual("dummy issue", issue.GetProperty(PropertyType.Description));

            Assert.AreEqual(Severity.Moderate.ToString(), issue.GetProperty(PropertyType.Severity));
            Assert.AreEqual(Area.CPU.ToString(), issue.GetProperty(PropertyType.Area));
            Assert.AreEqual("Assets/Dummy.cs:0", issue.GetProperty(PropertyType.Path));
            Assert.AreEqual("Dummy.cs:0", issue.GetProperty(PropertyType.Filename));
            Assert.AreEqual("cs", issue.GetProperty(PropertyType.FileType));
            Assert.AreEqual(Severity.Moderate.ToString(), issue.GetProperty(PropertyType.Severity));
            Assert.AreEqual(properties[0], issue.GetProperty(PropertyType.Num));
            Assert.AreEqual(properties[1], issue.GetProperty(PropertyType.Num + 1));
        }

        [Test]
        public void ProjectIssue_NoFileProperties_AreSet()
        {
            var issue = new ProjectIssue(IssueCategory.Code, m_Descriptor.id, "dummy issue");

            Assert.AreEqual(ProjectIssueExtensions.k_NotAvailable, issue.GetProperty(PropertyType.Path));
            Assert.AreEqual(ProjectIssueExtensions.k_NotAvailable, issue.GetProperty(PropertyType.Filename));
            Assert.AreEqual(ProjectIssueExtensions.k_NotAvailable, issue.GetProperty(PropertyType.FileType));
        }

        [Test]
        [TestCase(LogLevel.Error)]
        [TestCase(LogLevel.Warning)]
        [TestCase(LogLevel.Info)]
        public void ProjectIssue_Issue_IsCreatedWithLogLevel(LogLevel logLevel)
        {
            var context = new AnalysisContext();
            ProjectIssue issue = context.Create(IssueCategory.Code, m_Descriptor.id, "dummy issue")
                .WithLogLevel(logLevel);

            Assert.AreEqual(logLevel, issue.logLevel);
            Assert.AreEqual(logLevel.ToString(), issue.GetProperty(PropertyType.LogLevel));
        }
    }
}
