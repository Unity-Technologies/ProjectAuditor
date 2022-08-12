using System.Linq;
using NUnit.Framework;
using Unity.ProjectAuditor.Editor;

namespace Unity.ProjectAuditor.EditorTests
{
    class InstalledPackagesTests : TestFixtureBase
    {
        [Test]
        [TestCase("Project Auditor", "com.unity.project-auditor", "0.8.2-preview", "Local", new string[] { "com.unity.nuget.mono-cecil" })]
        [TestCase("Unity UI", "com.unity.ugui", "1.0.0", "BuiltIn", new string[] { "com.unity.modules.ui", "com.unity.modules.imgui" })]
        [TestCase("Unity Coding Tools", "com.unity.coding", "0.1.0-preview.22", "Registry")]
        public void InstallPackages_IsReported(string description, string name, string version, string source, string[] dependecies = null)
        {
            var installedPackagesIssue = Analyze(IssueCategory.installedPackages);
            var matchIssue = installedPackagesIssue.First(issue => issue.description == description);

            Assert.AreEqual(matchIssue.customProperties[0], name);
            Assert.AreEqual(matchIssue.customProperties[1], version);
            Assert.AreEqual(matchIssue.customProperties[2], source);
            if (dependecies != null && dependecies.Length != 0)
            {
                for (int i = 0; i < matchIssue.dependencies.GetNumChildren(); i++)
                {
                    Assert.IsTrue(matchIssue.dependencies.GetChild(i).GetName().Contains(dependecies[i]));
                }
            }
            Assert.NotNull(matchIssue);
        }
    }
}
