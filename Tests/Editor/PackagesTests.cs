using System.Linq;
using NUnit.Framework;
using Unity.ProjectAuditor.Editor;
using Unity.ProjectAuditor.Editor.Modules;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;

namespace Unity.ProjectAuditor.EditorTests
{
    class PackagesTests : TestFixtureBase
    {
        [OneTimeSetUp]
        public void SetUp()
        {
            var addRequest = Client.Add("com.unity.2d.pixel-perfect@3.0.2");
            while (!addRequest.IsCompleted)
                System.Threading.Thread.Sleep(10);
            Assert.True(addRequest.Status == StatusCode.Success, "Could not install the required package (com.unity.services.vivox). Make sure the package is able to be installed, and try again.");
            addRequest = Client.Add("com.unity.services.vivox@15.1.180001-pre.5");
            while (!addRequest.IsCompleted)
                System.Threading.Thread.Sleep(10);
            Assert.True(addRequest.Status == StatusCode.Success, "Could not install the required package (com.unity.services.vivox). Make sure the package is able to be installed, and try again.");
        }

        [OneTimeTearDown]
        public void TearDown()
        {
            var removeRequest = Client.Remove("com.unity.2d.pixel-perfect");
            while (!removeRequest.IsCompleted)
                System.Threading.Thread.Sleep(10);
            Assert.True(removeRequest.Status == StatusCode.Success, "Could not uninstall the required package (com.unity.2d.pixel-perfect). Make sure the package is able to be uninstall, and try again.");
            removeRequest = Client.Remove("com.unity.services.vivox");
            while (!removeRequest.IsCompleted)
                System.Threading.Thread.Sleep(10);
            Assert.True(removeRequest.Status == StatusCode.Success, "Could not uninstall the required package (com.unity.2d.pixel-perfect). Make sure the package is able to be uninstall, and try again.");
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
                Assert.AreNotEqual(string.Empty, package.description, "Package: " + package.GetCustomProperty(PackageProperty.Name));
                Assert.AreNotEqual(string.Empty, package.GetCustomProperty(PackageProperty.Name), "Package: " + package.description);
                Assert.AreNotEqual(string.Empty, package.GetCustomProperty(PackageProperty.Source), "Package: " + package.description);
                Assert.AreNotEqual(string.Empty, package.GetCustomProperty(PackageProperty.Version), "Package: " + package.description);
            }
        }

        [Test]
        [TestCase("Project Auditor", "com.unity.project-auditor", "Local", new string[] { "com.unity.nuget.mono-cecil" })]
        [TestCase("Audio", "com.unity.modules.audio", "BuiltIn")]
#if UNITY_2019_1_OR_NEWER
        [TestCase("Test Framework", "com.unity.test-framework", "Registry", new[] { "com.unity.ext.nunit", "com.unity.modules.imgui", "com.unity.modules.jsonserialize"})]
#endif
        public void Package_Installed_IsReported(string description, string name, string source, string[] dependencies = null)
        {
            var installedPackages = Analyze(IssueCategory.Package);
            var matchIssue = installedPackages.FirstOrDefault(issue => issue.description == description);

            Assert.IsNotNull(matchIssue, "Package {0} not found. Packages: {1}", description, string.Join(", ", installedPackages.Select(p => p.description).ToArray()));
            Assert.AreEqual(name, matchIssue.GetCustomProperty(PackageProperty.Name));
            Assert.AreEqual("Packages/" + name, matchIssue.location.Path);
            Assert.IsTrue(matchIssue.GetCustomProperty(PackageVersionProperty.RecommendedVersion).StartsWith(source), "Package: " + description);

            if (dependencies != null)
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
            var packageDiagnostics = Analyze(IssueCategory.PackageVersion);
            var diagnostic = packageDiagnostics.FirstOrDefault(issue => issue.GetCustomProperty(PackageVersionProperty.Name) == "com.unity.2d.pixel-perfect");

            Assert.IsNotNull(diagnostic, "Cannot find the upgrade package: com.unity.2d.pixel-perfect");
            Assert.AreEqual("com.unity.2d.pixel-perfect", diagnostic.GetCustomProperty(PackageVersionProperty.Name));
            Assert.AreEqual("3.0.2", diagnostic.GetCustomProperty(PackageVersionProperty.CurrentVersion));

            var currentVersion = diagnostic.GetCustomProperty(PackageVersionProperty.CurrentVersion);
            var recommendedVersion = diagnostic.GetCustomProperty(PackageVersionProperty.RecommendedVersion);

            Assert.AreNotEqual(currentVersion, recommendedVersion, "The current and recommended versions should be different");
        }

        [Test]
        public void Package_Preview_IsReported()
        {
            var packageDiagnostics = Analyze(IssueCategory.PackageVersion);
            var diagnostic = packageDiagnostics.FirstOrDefault(issue => issue.GetCustomProperty(PackageVersionProperty.Name) == "com.unity.services.vivox");

            Assert.IsNotNull(diagnostic, "Cannot find the upgrade package: com.unity.services.vivox");
            Assert.AreEqual("com.unity.services.vivox", diagnostic.GetCustomProperty(PackageVersionProperty.Name));
            Assert.AreEqual("15.1.180001-pre.5", diagnostic.GetCustomProperty(PackageVersionProperty.CurrentVersion));
            Assert.AreEqual(string.Empty, diagnostic.GetCustomProperty(PackageVersionProperty.RecommendedVersion));
            Assert.IsTrue(diagnostic.GetCustomPropertyAsBool(PackageVersionProperty.Experimental));
        }
    }
}
