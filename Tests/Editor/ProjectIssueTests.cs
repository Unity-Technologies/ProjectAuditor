using System;
using NUnit.Framework;
using Unity.ProjectAuditor.Editor;
using Unity.ProjectAuditor.Editor.CodeAnalysis;
using Unity.ProjectAuditor.Editor.Utils;

namespace Unity.ProjectAuditor.EditorTests
{
    class ProjectIssueTests
    {
        ProblemDescriptor s_Descriptor = new ProblemDescriptor
            (
            102001,
            "test",
            Area.CPU,
            "this is not actually a problem",
            "do nothing"
            );

        [Test]
        public void ProjectIssue_NewIssue_IsInitialized()
        {
            var description = "dummy issue";
            var uninitialised = new ProjectIssue(s_Descriptor, description, IssueCategory.Code);
            Assert.AreEqual(string.Empty, uninitialised.filename);
            Assert.AreEqual(string.Empty, uninitialised.relativePath);
            Assert.AreEqual(string.Empty, uninitialised.GetContext());
            Assert.AreEqual(description, uninitialised.description);
            Assert.False(uninitialised.isPerfCriticalContext);
        }

        [Test]
        public void ProjectIssue_CustomProperties_AreSet()
        {
            string[] properties =
            {
                "property #0",
                "property #1"
            };
            var issue = new ProjectIssue(s_Descriptor, "dummy issue", IssueCategory.Code);

            Assert.AreEqual(0, issue.GetNumCustomProperties());

            issue.SetCustomProperties(properties);

            Assert.AreEqual(2, issue.GetNumCustomProperties());
            Assert.AreEqual(properties[0], issue.GetCustomProperty(0));
            Assert.AreEqual(properties[1], issue.GetCustomProperty(1));
        }

        [Test]
        public void ProjectIssue_Properties_AreSet()
        {
            object[] properties =
            {
                "property #0",
                "property #1"
            };
            var issue = new ProjectIssue(s_Descriptor, "dummy issue", IssueCategory.Code)
            {
                location = new Location("Assets/Dummy.cs")
            };

            issue.SetCustomProperties(properties);

            Assert.AreEqual(2, issue.GetNumCustomProperties());
            Assert.AreEqual("dummy issue", issue.GetProperty(PropertyType.Description));

            Assert.AreEqual(Rule.Severity.Default.ToString(), issue.GetProperty(PropertyType.Severity));
            Assert.AreEqual(Area.CPU.ToString(), issue.GetProperty(PropertyType.Area));
            Assert.AreEqual("Assets/Dummy.cs:0", issue.GetProperty(PropertyType.Path));
            Assert.AreEqual("Dummy.cs:0", issue.GetProperty(PropertyType.Filename));
            Assert.AreEqual("cs", issue.GetProperty(PropertyType.FileType));
            Assert.AreEqual(false.ToString(), issue.GetProperty(PropertyType.CriticalContext));
            Assert.AreEqual(properties[0], issue.GetProperty(PropertyType.Num));
            Assert.AreEqual(properties[1], issue.GetProperty(PropertyType.Num + 1));
        }

        [Test]
        public void ProjectIssue_NoFileProperties_AreSet()
        {
            var issue = new ProjectIssue(s_Descriptor, "dummy issue", IssueCategory.Code);

            Assert.AreEqual(ProjectIssueExtensions.k_NotAvailable, issue.GetProperty(PropertyType.Path));
            Assert.AreEqual(ProjectIssueExtensions.k_NotAvailable, issue.GetProperty(PropertyType.Filename));
            Assert.AreEqual(ProjectIssueExtensions.k_NotAvailable, issue.GetProperty(PropertyType.FileType));
        }
    }
}
