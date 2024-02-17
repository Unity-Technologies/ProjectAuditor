using System;
using System.Collections;
using NUnit.Framework;
using Unity.ProjectAuditor.Editor;
using Unity.ProjectAuditor.Editor.Core;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace Unity.ProjectAuditor.EditorTests
{
    [Serializable]
    class ReportItemTests
    {
        readonly Descriptor m_Descriptor = new Descriptor
            (
            "TDD2001",
            "a title",
            Areas.CPU,
            "this is not actually a problem",
            "do nothing"
            );

        Descriptor m_DescriptorWithMessage = new Descriptor
            (
            "TDD2002",
            "a title",
            Areas.CPU,
            "this is not actually a problem",
            "do nothing"
            )
        {
            MessageFormat = "this is a message with argument {0}"
        };

        Descriptor m_CriticalIssueDescriptor = new Descriptor
            (
            "TDD2003",
            "a title of a critical problem",
            Areas.CPU,
            "this is not actually a problem",
            "do nothing"
            )
        {
            DefaultSeverity = Severity.Critical
        };

        [SerializeField]
        ReportItem m_Issue;

        [OneTimeSetUp]
        public void SetUp()
        {
            DescriptorLibrary.RegisterDescriptor(m_Descriptor.Id, m_Descriptor);
            DescriptorLibrary.RegisterDescriptor(m_DescriptorWithMessage.Id, m_DescriptorWithMessage);
            DescriptorLibrary.RegisterDescriptor(m_CriticalIssueDescriptor.Id, m_CriticalIssueDescriptor);
        }

        [Test]
        public void ProjectIssue_NewIssue_IsInitialized()
        {
            var description = "a title";
            var diagnostic = new ReportItem(IssueCategory.Code, description);
            Assert.AreEqual(string.Empty, diagnostic.Filename);
            Assert.AreEqual(string.Empty, diagnostic.RelativePath);
            Assert.AreEqual(string.Empty, diagnostic.GetContext());
            Assert.AreEqual(description, diagnostic.Description);
            Assert.AreNotEqual(Severity.Critical, diagnostic.Severity);

            Assert.IsFalse(diagnostic.IsMajorOrCritical());
        }

        [Test]
        public void ProjectIssue_NewIssue_IsNotFormatted()
        {
            var description = "a title";
            var diagnostic = new ReportItem(IssueCategory.Code, m_Descriptor.Id,  "dummy");
            Assert.AreEqual(string.Empty, diagnostic.Filename);
            Assert.AreEqual(string.Empty, diagnostic.RelativePath);
            Assert.AreEqual(string.Empty, diagnostic.GetContext());
            Assert.AreEqual(description, diagnostic.Description);
            Assert.AreNotEqual(Severity.Critical, diagnostic.Severity);

            Assert.IsFalse(diagnostic.IsMajorOrCritical());
        }

        [Test]
        public void ProjectIssue_NewIssue_IsFormatted()
        {
            var diagnostic = new ReportItem(IssueCategory.Code, m_DescriptorWithMessage.Id, "dummy");
            Assert.AreEqual(string.Empty, diagnostic.Filename);
            Assert.AreEqual(string.Empty, diagnostic.RelativePath);
            Assert.AreEqual(string.Empty, diagnostic.GetContext());
            Assert.AreEqual("this is a message with argument dummy", diagnostic.Description);
            Assert.AreNotEqual(Severity.Critical, diagnostic.Severity);

            Assert.IsFalse(diagnostic.IsMajorOrCritical());
        }

        [Test]
        public void ProjectIssue_NewIssue_IsCritical()
        {
            var description = "a title of a critical problem";
            var diagnostic = new ReportItem(IssueCategory.Code, m_CriticalIssueDescriptor.Id, description);
            Assert.AreEqual(string.Empty, diagnostic.Filename);
            Assert.AreEqual(string.Empty, diagnostic.RelativePath);
            Assert.AreEqual(string.Empty, diagnostic.GetContext());
            Assert.AreEqual(description, diagnostic.Description);

            // the issue should be critical as per the descriptor
            Assert.AreEqual(Severity.Critical, diagnostic.Severity);

            Assert.IsTrue(diagnostic.IsMajorOrCritical());
        }

        [UnityTest]
        public IEnumerator ProjectIssue_Priority_PersistsAfterDomainReload()
        {
            m_Issue = new ReportItem(IssueCategory.Code, m_Descriptor.Id);
            m_Issue.Severity = Severity.Major;

            EditorUtility.RequestScriptReload();
            yield return new WaitForDomainReload();

            Assert.AreEqual(Severity.Major, m_Issue.Severity);
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
            var issue = (ReportItem)context.CreateIssue(IssueCategory.Code, m_Descriptor.Id)
                .WithCustomProperties(properties);

            Assert.AreEqual(2, issue.GetNumCustomProperties());
            Assert.AreEqual(properties[0], issue.GetCustomProperty(0));
            Assert.AreEqual(properties[1], issue.GetCustomProperty(1));
        }

        [Test]
        public void ProjectIssue_CustomProperties_AreNotSet()
        {
            var issue = new ReportItem(IssueCategory.Code, m_Descriptor.Id);

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
            var description = "a title";
            var context = new AnalysisContext();
            var issue = (ReportItem)context.CreateIssue(IssueCategory.Code, m_Descriptor.Id)
                .WithCustomProperties(properties)
                .WithLocation("Assets/Dummy.cs");

            Assert.AreEqual(2, issue.GetNumCustomProperties());
            Assert.AreEqual(description, issue.GetProperty(PropertyType.Description));

            Assert.AreEqual(Severity.Moderate.ToString(), issue.GetProperty(PropertyType.Severity));
            Assert.AreEqual(Areas.CPU.ToString(), issue.GetProperty(PropertyType.Areas));
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
            var issue = new ReportItem(IssueCategory.Code, m_Descriptor.Id);

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
            ReportItem issue = context.CreateIssue(IssueCategory.Code, m_Descriptor.Id)
                .WithLogLevel(logLevel);

            Assert.AreEqual(logLevel, issue.LogLevel);
            Assert.AreEqual(logLevel.ToString(), issue.GetProperty(PropertyType.LogLevel));
        }
    }
}
