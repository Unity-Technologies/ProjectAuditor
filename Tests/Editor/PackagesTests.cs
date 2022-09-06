using System.Linq;
using NUnit.Framework;
using Unity.ProjectAuditor.Editor;
using Unity.ProjectAuditor.Editor.Modules;

namespace Unity.ProjectAuditor.EditorTests
{
    class PackagesTests : TestFixtureBase
    {
        [Test]
        public void InstalledPackages_AreValid()
        {
            var installedPackages = Analyze(IssueCategory.Package);
#if !UNITY_2019_1_OR_NEWER
            // for some reason com.unity.ads is missing the description in 2018.x
            installedPackages = installedPackages.Where(p => !p.GetCustomProperty(PackageProperty.Name).Equals("com.unity.ads")).ToArray();
#endif
            foreach (var package in installedPackages)
            {
                Assert.AreNotEqual(string.Empty, package.description, "Package: " + package.GetCustomProperty(PackageProperty.Name));
                Assert.AreNotEqual(string.Empty, package.GetCustomProperty(PackageProperty.Name), "Package: " + package.description);
                Assert.AreNotEqual(string.Empty, package.GetCustomProperty(PackageProperty.Source), "Package: " + package.description);
                Assert.AreNotEqual(string.Empty, package.GetCustomProperty(PackageProperty.Version), "Package: " + package.description);
            }
        }

        [Test]
        [TestCase("Project Auditor", "com.unity.project-auditor", "Local", new string[] { "com.unity.nuget.mono-cecil" })]
#if UNITY_2019_1_OR_NEWER
        [TestCase("Unity UI", "com.unity.ugui", "BuiltIn", new string[] { "com.unity.modules.ui", "com.unity.modules.imgui" })]
        [TestCase("Test Framework", "com.unity.test-framework", "Registry")]
#endif
        public void InstalledPackage_IsReported(string description, string name, string source, string[] dependencies = null)
        {
            var installedPackages = Analyze(IssueCategory.Package);
            var matchIssue = installedPackages.FirstOrDefault(issue => issue.description == description);

            Assert.IsNotNull(matchIssue, "Cannot find the package: " + description);
            Assert.AreEqual(matchIssue.customProperties[0], name);
            Assert.IsTrue(matchIssue.customProperties[2].StartsWith(source), "Package: " + description);

            if (dependencies != null && dependencies.Length != 0)
            {
                for (var i = 0; i < dependencies.Length; i++)
                {
                    Assert.IsTrue(matchIssue.dependencies.GetChild(i).GetName().Contains(dependencies[i]), "Package: " + description);
                }
            }
        }
    }
}
