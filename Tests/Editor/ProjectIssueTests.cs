using System;
using NUnit.Framework;
using Unity.ProjectAuditor.Editor;
using Unity.ProjectAuditor.Editor.CodeAnalysis;

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
            var uninitialised = new ProjectIssue(s_Descriptor, "dummy issue", IssueCategory.Code);
            Assert.AreEqual(string.Empty, uninitialised.filename);
            Assert.AreEqual(string.Empty, uninitialised.relativePath);
            Assert.AreEqual(string.Empty, uninitialised.GetCallingMethod());
            Assert.AreEqual(string.Empty, uninitialised.name);
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
    }
}
