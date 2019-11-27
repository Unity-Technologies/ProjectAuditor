using NUnit.Framework;
using Unity.ProjectAuditor.Editor;

namespace UnityEditor.ProjectAuditor.EditorTests
{
	class ProblemDescriptorTest {

		[Test]
		public void UninitializedProblemDescriptorTestPasses()
		{
			var uninitialised = new ProblemDescriptor();
			Assert.AreEqual(string.Empty, uninitialised.description);
		}
	}	
}

