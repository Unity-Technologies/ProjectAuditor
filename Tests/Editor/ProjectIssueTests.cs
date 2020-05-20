using NUnit.Framework;
using Unity.ProjectAuditor.Editor;

namespace UnityEditor.ProjectAuditor.EditorTests
{
    internal class ProjectIssueTests
    {
        [Test]
        public void UninitializedIssueTestPasses()
        {
            var p = new ProblemDescriptor
                (
                102001,
                "test",
                Area.CPU,
                "this is not actually a problem",
                "do nothing"
                );
            var uninitialised = new ProjectIssue(p, "dummy issue", IssueCategory.ApiCalls);
            Assert.AreEqual(string.Empty, uninitialised.filename);
            Assert.AreEqual(string.Empty, uninitialised.relativePath);
            Assert.AreEqual(string.Empty, uninitialised.callingMethod);
            Assert.AreEqual(string.Empty, uninitialised.name);
            Assert.False(uninitialised.isPerfCriticalContext);
        }
    }
}
