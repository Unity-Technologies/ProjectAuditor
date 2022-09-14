using System.Linq;
using NUnit.Framework;
using Unity.ProjectAuditor.Editor;
using Unity.ProjectAuditor.Editor.Modules;
using UnityEditor.Compilation;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;

namespace Unity.ProjectAuditor.EditorTests
{
    class PackagesTests : TestFixtureBase
    {
        [OneTimeSetUp]
        public void SetUp()
        {
            AddRequest AddRequest = Client.Add("com.unity.2d.pixel-perfect@3.0.2");
            while (AddRequest.Status != StatusCode.Success) {}
            AddRequest = Client.Add("com.unity.services.vivox");
            while (AddRequest.Status != StatusCode.Success) {}
        }

        [OneTimeTearDown]
        public void TearDown()
        {
            RemoveRequest removeRequest = Client.Remove("com.unity.2d.pixel-perfect");
            while (removeRequest.Status != StatusCode.Success) {}
            removeRequest = Client.Remove("com.unity.services.vivox");
            while (removeRequest.Status != StatusCode.Success) {}
        }

        [Test]
        public void Packages_Installed_AreValid()
        {
            var installedPackages = Analyze(IssueCategory.Package);
#if !UNITY_2019_1_OR_NEWER
            // for some reason com.unity.ads is missing the description in 2018.x
            installedPackages = installedPackages.Where(p => !p.GetCustomProperty(PackageProperty.PackageID).Equals("com.unity.ads")).ToArray();
#endif
            foreach (var package in installedPackages)
            {
                Assert.AreNotEqual(string.Empty, package.description, "Package: " + package.GetCustomProperty(PackageProperty.PackageID));
                Assert.AreNotEqual(string.Empty, package.GetCustomProperty(PackageProperty.PackageID), "Package: " + package.description);
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
        public void Package_Installed_IsReported(string description, string name, string source, string[] dependencies = null)
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

        [Test]
        public void Package_Upgrade_IsRecommended()
        {
            var issuePackages = Analyze(IssueCategory.PackageVersion);
            var matchIssue = issuePackages.FirstOrDefault(issue => issue.customProperties[0] == "com.unity.2d.pixel-perfect");

            Assert.IsNotNull(matchIssue, "Cannot find the upgrade package: com.unity.2d.pixel-perfect");
            Assert.AreEqual(matchIssue.GetCustomProperty(PackageVersionProperty.PackageID), "com.unity.2d.pixel-perfect");
            Assert.AreEqual(matchIssue.GetCustomProperty(PackageVersionProperty.CurrentVersion), "3.0.2");
            var currentVersion = System.Version.Parse(matchIssue.GetCustomProperty(PackageVersionProperty.CurrentVersion));
            var recommendedVersion = System.Version.Parse(matchIssue.GetCustomProperty(PackageVersionProperty.RecommendedVersion));
            Assert.IsTrue(recommendedVersion.CompareTo(currentVersion) > 0, "The recommended version is wrong");
            Assert.AreEqual(matchIssue.GetCustomProperty(PackageVersionProperty.Experimental), "False");
        }

        [Test]
        public void Package_Preview_IsReported()
        {
            var issuePackages = Analyze(IssueCategory.PackageVersion);
            var matchIssue = issuePackages.FirstOrDefault(issue => issue.customProperties[0] == "com.unity.services.vivox");

            Assert.IsNotNull(matchIssue, "Cannot find the upgrade package: com.unity.services.vivox");
            Assert.AreEqual(matchIssue.GetCustomProperty(PackageVersionProperty.PackageID), "com.unity.services.vivox");
            Assert.AreEqual(matchIssue.GetCustomProperty(PackageVersionProperty.CurrentVersion), "15.1.180001-pre.5");
            Assert.AreEqual(matchIssue.GetCustomProperty(PackageVersionProperty.RecommendedVersion), "");
            Assert.AreEqual(matchIssue.GetCustomProperty(PackageVersionProperty.Experimental), "True");
        }
    }
}
