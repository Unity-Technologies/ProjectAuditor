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
#if UNITY_2019_1_OR_NEWER
            AddPackage("com.unity.2d.pixel-perfect@3.0.2");
#endif
            AddPackage("com.unity.services.vivox@15.1.180001-pre.5");
        }

        [OneTimeTearDown]
        public void TearDown()
        {
#if UNITY_2019_1_OR_NEWER
            RemovePackage("com.unity.2d.pixel-perfect");
#endif
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
#if UNITY_2019_1_OR_NEWER
        [TestCase("Test Framework", "com.unity.test-framework", PackageSource.Registry, new[] { "com.unity.ext.nunit", "com.unity.modules.imgui", "com.unity.modules.jsonserialize"})]
        [TestCase("Project Auditor", "com.unity.project-auditor", PackageSource.LocalTarball, new string[] { "com.unity.nuget.mono-cecil" })]
#else
        [TestCase("Project Auditor", "com.unity.project-auditor", PackageSource.Local, new string[] { "com.unity.nuget.mono-cecil" })]
#endif
        [TestCase("Audio", "com.unity.modules.audio", PackageSource.BuiltIn)]
        public void Package_Installed_IsReported(string description, string name, PackageSource source, string[] dependencies = null)
        {
            var installedPackages = Analyze(IssueCategory.Package);
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
        }

        [Test]
        public void Package_Preview_IsReported()
        {
            var packageDiagnostics = Analyze(IssueCategory.PackageDiagnostic);
            var diagnostic = packageDiagnostics.FirstOrDefault(issue => issue.description.Contains("com.unity.services.vivox"));

            Assert.IsNotNull(diagnostic, "Cannot find the upgrade package: com.unity.services.vivox");
            Assert.IsTrue(diagnostic.description.StartsWith("'com.unity.services.vivox' version "), "Description: " + diagnostic.description);
        }
    }
}
