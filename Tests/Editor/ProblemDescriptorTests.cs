using NUnit.Framework;
using Unity.ProjectAuditor.Editor;

namespace UnityEditor.ProjectAuditor.EditorTests
{
    internal class ProblemDescriptorTests
    {
        [Test]
        public void UninitializedProblemDescriptorTestPasses()
        {
            // not much value in this test yet
            var uninitialised = new ProblemDescriptor();
            Assert.Null(uninitialised.description);
        }
    }
}
