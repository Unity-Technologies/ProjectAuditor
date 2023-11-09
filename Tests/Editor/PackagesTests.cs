using System.Linq;
using NUnit.Framework;
using Unity.ProjectAuditor.Editor;
using Unity.ProjectAuditor.Editor.Diagnostic;
using Unity.ProjectAuditor.Editor.Modules;
using Unity.ProjectAuditor.Editor.Tests.Common;
using Unity.ProjectAuditor.Editor.Utils;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;

namespace Unity.ProjectAuditor.EditorTests
{
    class PackagesTests : TestFixtureBase
    {
        [OneTimeSetUp]
        public void SetUp()
        {
#if UNITY_2019_1_OR_NEWER
            AddPackage("com.unity.2d.pixel-perfect@3.0.2");
            AddPackage("com.unity.services.vivox");
#endif
        }

        [OneTimeTearDown]
        public void TearDown()
        {
#if UNITY_2019_1_OR_NEWER
            RemovePackage("com.unity.2d.pixel-perfect");
            RemovePackage("com.unity.services.vivox");
#endif
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
#if !UNITY_2019_1_OR_NEWER
            // for some reason com.unity.ads is missing the description in 2018.x
            installedPackages = installedPackages.Where(p => !p.GetCustomProperty(PackageProperty.Name).Equals("com.unity.ads")).ToArray();
#endif
            foreach (var package in installedPackages)
            {
                var name = package.GetCustomProperty(PackageProperty.Name);
                if (name.Equals("com.unity.project-auditor.tests"))
                    continue;
                Assert.AreNotEqual(string.Empty, package.description, "Package: " + package.GetCustomProperty(PackageProperty.Name));
                Assert.AreNotEqual(string.Empty, package.GetCustomProperty(PackageProperty.Name), "Package: " + package.description);
                Assert.AreNotEqual(string.Empty, package.GetCustomProperty(PackageProperty.Source), "Package: " + package.description);
                Assert.AreNotEqual(string.Empty, package.GetCustomProperty(PackageProperty.Version), "Package: " + package.description);
            }
        }

        [Test]
#if UNITY_2019_1_OR_NEWER
        [TestCase("Test Framework", "com.unity.test-framework", PackageSource.Registry, new[] { "com.unity.ext.nunit", "com.unity.modules.imgui", "com.unity.modules.jsonserialize"})]
        [TestCase("Project Auditor", Editor.ProjectAuditor.k_PackageName, PackageSource.LocalTarball, new string[] { "com.unity.nuget.mono-cecil" })]
#else
        [TestCase("Project Auditor", Editor.ProjectAuditor.k_PackageName, PackageSource.Unknown, new string[] { "com.unity.nuget.mono-cecil" })]
#endif
        [TestCase("Audio", "com.unity.modules.audio", PackageSource.BuiltIn)]
        public void Package_Installed_IsReported(string description, string name, PackageSource source, string[] dependencies = null)
        {
            var installedPackages = Analyze(IssueCategory.Package);
            if (name.Equals(Editor.ProjectAuditor.k_PackageName) &&
                !PackageUtils.IsClientPackage(Editor.ProjectAuditor.k_PackageName))
            {
                return;
            }

            var package = installedPackages.FirstOrDefault(issue => issue.description == description);

            Assert.IsNotNull(package, "Package {0} not found. Packages: {1}", description, string.Join(", ", installedPackages.Select(p => p.description).ToArray()));
            Assert.AreEqual(name, package.GetCustomProperty(PackageProperty.Name));
            Assert.AreEqual(source.ToString(), package.GetCustomProperty(PackageProperty.Source));
            Assert.AreEqual("Packages/" + name, package.location.Path);

            if (dependencies != null)
            {
                for (var i = 0; i < dependencies.Length; i++)
                {
                    Assert.IsTrue(package.dependencies.GetChild(i).GetName().Contains(dependencies[i]), "Package: " + description);
                }
            }
        }

        [Test]
#if !UNITY_2019_1_OR_NEWER
        [Ignore("Package version is not available in 2018.4")]
#endif
        public void Package_Upgrade_IsRecommended()
        {
            var packageDiagnostics = Analyze(IssueCategory.PackageDiagnostic);
            var diagnostic = packageDiagnostics.FirstOrDefault(issue => issue.description.Contains("com.unity.2d.pixel-perfect"));

            Assert.IsNotNull(diagnostic, "Cannot find the upgrade package: com.unity.2d.pixel-perfect");
            Assert.IsTrue(diagnostic.description.StartsWith("'com.unity.2d.pixel-perfect' could be updated from version '3.0.2' to "), "Description: " + diagnostic.description);
            Assert.AreEqual(Severity.Minor, diagnostic.severity);
        }

        [Test]
#if !UNITY_2019_1_OR_NEWER
        [Ignore("Package dependency com.unity.services.core does not compile in 2018.4")]
#endif
        public void Package_Preview_IsReported()
        {
            var packageDiagnostics = Analyze(IssueCategory.PackageDiagnostic);
            var diagnostic = packageDiagnostics.FirstOrDefault(issue => issue.description.Contains("com.unity.services.vivox"));

            Assert.IsNotNull(diagnostic, "Cannot find the upgrade package: com.unity.services.vivox");
            Assert.IsTrue(diagnostic.description.StartsWith("'com.unity.services.vivox' version "), "Description: " + diagnostic.description);
            Assert.AreEqual(Severity.Moderate, diagnostic.severity);
        }

        [Test]
        [TestCase("com.unity.nuget.mono-cecil")]
        public void PackageUtils_Package_IsInstalled(string packageName)
        {
            Assert.IsTrue(PackageUtils.IsClientPackage(packageName), $"Package {packageName} is not installed");
        }

        [Test]
        public void PackageUtils_PackageVersions_AreCompared()
        {
            Assert.AreEqual(-1, PackageUtils.CompareVersions("1.0.1", "1.0.2"));
            Assert.AreEqual(0, PackageUtils.CompareVersions("1.0.3-pre", "1.0.3"));
            Assert.AreEqual(1, PackageUtils.CompareVersions("1.0.2", "1.0.1"));
            Assert.AreEqual(1, PackageUtils.CompareVersions("1.1.0", "1.0.8"));
            Assert.AreEqual(1, PackageUtils.CompareVersions("2.0.2", "1.1.0"));
            Assert.AreEqual(1, PackageUtils.CompareVersions("1.8.0-pre.20", "1.8.0-pre.1"));
        }
    }
}
