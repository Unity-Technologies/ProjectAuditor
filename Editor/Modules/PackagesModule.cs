using System.Collections;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using System.Linq;
using UnityEditor;


namespace Unity.ProjectAuditor.Editor.Modules
{
    public enum PackageProperty
    {
        Name = 0,
        Version,
        Source,
        Num
    }

    public enum PackageVersionProperty
    {
        Name = 0,
        CurrentVersion,
        ReconmandVersion,
        Experimental,
        Num
    }

    class PackagesModule : ProjectAuditorModule
    {
        static readonly IssueLayout k_PackageLayout = new IssueLayout
        {
            category = IssueCategory.Package,
            properties = new[]
            {
                new PropertyDefinition { type = PropertyType.Description, name = "Display Name", },
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(PackageProperty.Name), format = PropertyFormat.String, name = "Name" },
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(PackageProperty.Version), format = PropertyFormat.String, name = "Version" },
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(PackageProperty.Source), format = PropertyFormat.String, name = "Source", defaultGroup = true }
            }
        };

        static readonly IssueLayout k_PacakgeVersionLayout = new IssueLayout
        {
            category = IssueCategory.PackageVersion,
            properties = new[]
            {
                new PropertyDefinition { type = PropertyType.Description, name = "Display Name"},
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(PackageVersionProperty.Name), format = PropertyFormat.String, name = "Package Name" },
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(PackageVersionProperty.CurrentVersion), format = PropertyFormat.String, name = "Current Version" },
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(PackageVersionProperty.ReconmandVersion), format = PropertyFormat.String, name = "Reconmand Version" , defaultGroup = true},
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(PackageVersionProperty.Experimental), format = PropertyFormat.Bool, name = "Preview" }
            }
        };


        static readonly ProblemDescriptor k_recommendPacakgeUpgrade  = new ProblemDescriptor(
            "PKG0001",
            "package name",
            new[] { Area.BuildSize },
            "A newer version of this package is available",
            "we strongly encourage you to update from the Unity Package Manager."
        );

        static readonly ProblemDescriptor k_recommendPacakgePreView = new ProblemDescriptor(
            "PKG0002",
            "package name",
            new[] { Area.BuildSize },
            "Preview Packages are in the early stages of development and not yet ready for production. We recommend using these only for testing purposes and to give us direct feedback"
        );

        public override void Audit(ProjectAuditorParams projectAuditorParams, IProgress progress = null)
        {
            var request = Client.List();
            while (request.Status != StatusCode.Success) {}
            var issues = new List<ProjectIssue>();
            foreach (var package in request.Result)
            {
                AddInstalledPackage(package, issues);
                AddPackageVersionIssue(package, issues);
            }
            if (issues.Count > 0)
                projectAuditorParams.onIncomingIssues(issues);
            projectAuditorParams.onModuleCompleted?.Invoke();
        }

        void AddInstalledPackage(UnityEditor.PackageManager.PackageInfo package, List<ProjectIssue> issues)
        {
            var dependencies = package.dependencies.Select(d => d.name + " [" + d.version + "]").ToArray();
            var node = new PackageDependencyNode(package.displayName, dependencies);
            var packageIssue = ProjectIssue.Create(IssueCategory.Package, package.displayName).WithCustomProperties(new object[(int)PackageProperty.Num]
            {
                package.name,
                package.version,
                package.source
            }).WithDependencies(node);
            issues.Add(packageIssue);
        }

        void AddPackageVersionIssue(UnityEditor.PackageManager.PackageInfo package, List<ProjectIssue> issues)
        {
            var result = 0;
            var isPreview = false;
            if (!String.IsNullOrEmpty(package.version) && !String.IsNullOrEmpty(package.versions.verified))
            {
                var currentVersion = new Version(package.version);
                var recommandVersion = new Version(package.versions.verified);
                result = currentVersion.CompareTo(recommandVersion);
            }

            if (package.version.Contains("pre") || package.version.Contains("exp"))
            {
                isPreview = true;
            }
            if (result < 0 || isPreview)
            {
                var packageVersionIssue = ProjectIssue.Create(IssueCategory.PackageVersion, isPreview ? k_recommendPacakgePreView : k_recommendPacakgeUpgrade, package.displayName).WithCustomProperties(new object[(int)PackageVersionProperty.Num]
                {
                    package.name,
                    package.version,
                    package.versions.verified,
                    isPreview
                });
                issues.Add(packageVersionIssue);
            }
        }

        public override IEnumerable<IssueLayout> GetLayouts()
        {
            yield return k_PackageLayout;
            yield return k_PacakgeVersionLayout;
        }
    }
}
