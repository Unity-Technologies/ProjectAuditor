using System.Linq;
using NUnit.Framework;
using Unity.ProjectAuditor.Editor;
using Unity.ProjectAuditor.Editor.Modules;
using Unity.ProjectAuditor.Editor.Tests.Common;
using Unity.ProjectAuditor.Editor.Utils;
using UnityEditor.PackageManager;

namespace Unity.ProjectAuditor.EditorTests
{
    class PackagesTests : TestFixtureBase
    {
        [OneTimeSetUp]
        public void SetUp()
        {
            AddPackage("com.unity.2d.pixel-perfect@3.0.2");
            AddPackage("com.unity.services.vivox@15.1.210100-pre.1");
        }

        [OneTimeTearDown]
        public void TearDown()
        {
            RemovePackage("com.unity.2d.pixel-perfect");
            RemovePackage("com.unity.services.vivox");
        }

        void AddPackage(string packageIdOrName)
        {
            var addRequest = Client.Add(packageIdOrName);
            while (!addRequest.IsCompleted)
                System.Threading.Thread.Sleep(10);
            Assert.True(addRequest.Status == StatusCode.Success, $"Could not install the required package ({packageIdOrName}). Make sure the package is able to be installed, and try again.");
        }

        void RemovePackage(string packageName)
        {
            var removeRequest = Client.Remove(packageName);
            while (!removeRequest.IsCompleted)
                System.Threading.Thread.Sleep(10);
            Assert.True(removeRequest.Status == StatusCode.Success, $"Could not uninstall the required package ({packageName}). Make sure the package is able to be uninstall, and try again.");
        }

        [Test]
        public void Packages_Installed_AreValid()
        {
            var installedPackages = Analyze(IssueCategory.Package);
            foreach (var package in installedPackages)
            {
                var name = package.GetCustomProperty(PackageProperty.Name);
                if (name.Equals("com.unity.project-auditor.tests"))
                    continue;
                Assert.AreNotEqual(string.Empty, package.Description, "Package: " + package.GetCustomProperty(PackageProperty.Name));
                Assert.AreNotEqual(string.Empty, package.GetCustomProperty(PackageProperty.Name), "Package: " + package.Description);
                Assert.AreNotEqual(string.Empty, package.GetCustomProperty(PackageProperty.Source), "Package: " + package.Description);
                Assert.AreNotEqual(string.Empty, package.GetCustomProperty(PackageProperty.Version), "Package: " + package.Description);
            }
        }

        [Test]
        [TestCase("Test Framework", "com.unity.test-framework", PackageSource.Registry, new[] { "com.unity.ext.nunit", "com.unity.modules.imgui", "com.unity.modules.jsonserialize"})]
        [TestCase("Project Auditor", ProjectAuditorPackage.Name, PackageSource.LocalTarball, new string[] { "com.unity.nuget.mono-cecil" })]
        [TestCase("Audio", "com.unity.modules.audio", PackageSource.BuiltIn)]
        public void Package_Installed_IsReported(string description, string name, PackageSource source, string[] dependencies = null)
        {
            var installedPackages = Analyze(IssueCategory.Package);
            if (name.Equals(ProjectAuditorPackage.Name) &&
                !ProjectAuditorPackage.IsLocal)
            {
                return;
            }

            var package = installedPackages.FirstOrDefault(issue => issue.Description == description);

            Assert.IsNotNull(package, "Package {0} not found. Packages: {1}", description, string.Join(", ", installedPackages.Select(p => p.Description).ToArray()));
            Assert.AreEqual(name, package.GetCustomProperty(PackageProperty.Name));
            Assert.AreEqual(source.ToString(), package.GetCustomProperty(PackageProperty.Source));
            Assert.AreEqual("Packages/" + name, package.Location.Path);

            if (dependencies != null)
            {
                for (var i = 0; i < dependencies.Length; i++)
                {
                    Assert.IsTrue(package.Dependencies.GetChild(i).GetName().Contains(dependencies[i]), "Package: " + description);
                }
            }
        }

        [Test]
        [TestCase("com.unity.2d.pixel-perfect")]
        public void Package_Upgrade_IsRecommended(string packageName)
        {
            var issues = Analyze(IssueCategory.ProjectSetting);
            var packageIssue = issues.FirstOrDefault(issue => issue.Description.Contains(packageName));

            Assert.IsNotNull(packageIssue, $"Cannot find package diagnostic for: {packageName}");
            Assert.IsTrue(packageIssue.Description.StartsWith($"Package '{packageName}' could be updated from version '3.0.2' to "), "Description: " + packageIssue.Description);
            Assert.AreEqual(Severity.Minor, packageIssue.Severity);
        }

        [Test]
        [TestCase("com.unity.services.vivox")]
        public void Package_Preview_IsReported(string packageName)
        {
            var issues = Analyze(IssueCategory.ProjectSetting);
            var packageIssue = issues.FirstOrDefault(issue => issue.Description.Contains(packageName));

            Assert.IsNotNull(packageIssue, $"Cannot find package diagnostic for: {packageName}");
            Assert.IsTrue(packageIssue.Description.StartsWith($"Package '{packageName}' version "), "Description: " + packageIssue.Description);
            Assert.AreEqual(Severity.Moderate, packageIssue.Severity);
        }

        [Test]
        [TestCase("com.unity.nuget.mono-cecil")]
        public void PackageUtils_Package_IsInstalled(string packageName)
        {
            Assert.IsTrue(PackageUtils.IsClientPackage(packageName), $"Package {packageName} is not installed");
        }

        [TestCase(-1, "1.0.1", "1.0.2")]
        [TestCase(0, "1.0.3-pre", "1.0.3")]
        [TestCase(1, "1.0.2", "1.0.1")]
        [TestCase(1, "1.1.0", "1.0.8")]
        [TestCase(1, "2.0.2", "1.1.0")]
        [TestCase(1, "1.8.0-pre.20", "1.8.0-pre.1")]
        public void PackageUtils_PackageVersions_AreCompared(int expected, string version1, string version2)
        {
            Assert.AreEqual(expected, PackageUtils.CompareVersions(version1, version2));
        }
    }
}
