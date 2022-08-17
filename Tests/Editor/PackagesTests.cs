using System.Linq;
using NUnit.Framework;
using Unity.ProjectAuditor.Editor;

namespace Unity.ProjectAuditor.EditorTests
{
    class PackagesTests : TestFixtureBase
    {
        [Test]
        [TestCase("Project Auditor", "com.unity.project-auditor", "Local", new string[] { "com.unity.nuget.mono-cecil" })]
        [TestCase("Physics", "com.unity.modules.physics", "BuiltIn", new string[] { "com.unity.modules.ui", "com.unity.modules.imgui" })]
        [TestCase("Test Framework", "com.unity.test-framework", "Registry")]
        public void InstalledPackage_IsReported(string description, string name, string source, string[] dependencies = null)
        {
            var installedPackages = Analyze(IssueCategory.Package);
            var matchIssue = installedPackages.FirstOrDefault(issue => issue.description == description);
            Assert.IsNotNull(matchIssue, "Cannot find the package: " + description);
            Assert.AreEqual(matchIssue.customProperties[0], name);
            Assert.IsTrue(matchIssue.customProperties[2].StartsWith(source), "Package: " + description);
            if (dependencies != null && dependencies.Length != 0)
            {
                for (int i = 0; i < matchIssue.dependencies.GetNumChildren(); i++)
                {
                    Assert.IsTrue(matchIssue.dependencies.GetChild(i).GetName().Contains(dependencies[i]), "Package: " + description);
                }
            }
        }
    }
}
