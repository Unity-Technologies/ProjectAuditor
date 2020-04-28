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

        [Test]
        public void ProblemDescriptorsAreEqual()
        {
            var a = new ProblemDescriptor
            {
                id = 102001
            };
            var b = new ProblemDescriptor
            {
                id = 102001
            };

            Assert.True(a.Equals(a));
            Assert.True(a.Equals((object)a));
            Assert.True(a.Equals(b));
            Assert.True(a.Equals((object)b));
            b = null;
            Assert.False(a.Equals(b));
            Assert.False(a.Equals((object)b));
        }

        [Test]
        public void ProblemDescriptorHashIsId()
        {
            var p = new ProblemDescriptor
            {
                id = 102001
            };

            Assert.True(p.GetHashCode() == p.id);
        }
    }
}
