using NUnit.Framework;
using Unity.ProjectAuditor.Editor;

namespace UnityEditor.ProjectAuditor.EditorTests
{
	class ProblemDescriptorTest
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

