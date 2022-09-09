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
        public void InstalledPackages_AreValid()
        {
            var installedPackages = Analyze(IssueCategory.Package);
#if !UNITY_2019_1_OR_NEWER
            // for some reason com.unity.ads is missing the description in 2018.x
            installedPackages = installedPackages.Where(p => !p.GetCustomProperty(PackageProperty.Name).Equals("com.unity.ads")).ToArray();
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

        [Test]
        public void RecommandedUpgradePackage_IsReproted()
        {
            var issuePackages = Analyze(IssueCategory.PackageVersion);
            var matchIssue = issuePackages.FirstOrDefault(issue => issue.customProperties[0] == "com.unity.2d.pixel-perfect");

            Assert.IsNotNull(matchIssue, "Cannot find the upgrade pacakge: com.unity.2d.pixel-perfect");
            Assert.AreEqual(matchIssue.customProperties[0], "com.unity.2d.pixel-perfect");
            Assert.AreEqual(matchIssue.customProperties[1], "3.0.2");
            Assert.AreEqual(matchIssue.customProperties[2], "4.0.1");
            Assert.AreEqual(matchIssue.customProperties[3], "False");
        }

        [Test]
        public void RecommandedPreviewPackage_IsReproted()
        {
            var issuePackages = Analyze(IssueCategory.PackageVersion);
            var matchIssue = issuePackages.FirstOrDefault(issue => issue.customProperties[0] == "com.unity.services.vivox");

            Assert.IsNotNull(matchIssue, "Cannot find the upgrade pacakge: com.unity.services.vivox");
            Assert.AreEqual(matchIssue.customProperties[0], "com.unity.services.vivox");
            Assert.AreEqual(matchIssue.customProperties[1], "15.1.180001-pre.5");
            Assert.AreEqual(matchIssue.customProperties[2], "");
            Assert.AreEqual(matchIssue.customProperties[3], "True");
        }
    }
}
