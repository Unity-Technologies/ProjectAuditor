using System;
using System.Collections.Generic;
using UnityEditor.PackageManager;
using System.Linq;
using Unity.ProjectAuditor.Editor.Core;
using Unity.ProjectAuditor.Editor.Utils;
using UnityEditor;
using UnityEngine;


namespace Unity.ProjectAuditor.Editor.Modules
{
    public enum PackageProperty
    {
        PackageID = 0,
        Version,
        Source,
        Num
    }

    public enum PackageVersionProperty
    {
        PackageID = 0,
        CurrentVersion,
        RecommendedVersion,
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
                new PropertyDefinition { type = PropertyType.Description, name = "Name", longName = "Package Name" },
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(PackageProperty.PackageID), format = PropertyFormat.String, name = "ID", longName = "Package ID" },
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(PackageProperty.Version), format = PropertyFormat.String, name = "Version" },
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(PackageProperty.Source), format = PropertyFormat.String, name = "Source", defaultGroup = true }
            }
        };


        static readonly IssueLayout k_PackageVersionLayout = new IssueLayout
        {
            category = IssueCategory.PackageVersion,
            properties = new[]
            {
                new PropertyDefinition { type = PropertyType.Description, name = "Name", longName = "Package Name"},
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(PackageVersionProperty.PackageID), format = PropertyFormat.String, name = "ID", longName = "Package ID", defaultGroup = true},
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(PackageVersionProperty.CurrentVersion), format = PropertyFormat.String, name = "Current Version" },
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(PackageVersionProperty.RecommendedVersion), format = PropertyFormat.String, name = "Recommended Version"},
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(PackageVersionProperty.Experimental), format = PropertyFormat.Bool, name = "Experimental/Preview" }
            }
        };


        static readonly ProblemDescriptor k_RecommendPackageUpgrade  = new ProblemDescriptor(
            "PAP0001",
            "Upgradable packages",
            new[] { Area.Quality },
            "A newer version of this package is available",
            "we strongly encourage you to update from the Unity Package Manager."
        );

        static readonly ProblemDescriptor k_RecommendPackagePreView = new ProblemDescriptor(
            "PAP0002",
            "Experimental/Preview packages",
            new[] { Area.Quality },
            "Preview Packages are in the early stages of development and not yet ready for production. We recommend using these only for testing purposes and to give us direct feedback"
        );

        public override string name => "Packages";

        public override IReadOnlyCollection<IssueLayout> supportedLayouts => new IssueLayout[]
        {
            k_PackageLayout,
            k_PackageVersionLayout
        };

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
            var recommendedVersionString = PackageUtils.GetPackageRecommendedVersion(package);
            if (!String.IsNullOrEmpty(package.version) && !String.IsNullOrEmpty(recommendedVersionString))
            {
                try
                {
                    var currentVersion = new Version(package.version);
                    var recommendedVersion = new Version(recommendedVersionString);
                    result = currentVersion.CompareTo(recommendedVersion);
                }
                catch (ArgumentException)
                {
                    Debug.LogWarningFormat("Package '{0}' with incorrect version format: {1}", package.name, package.version);
                }
            }

            if (package.version.Contains("pre") || package.version.Contains("exp"))
            {
                isPreview = true;
            }
            if (result < 0 || isPreview)
            {
                var packageVersionIssue = ProjectIssue.Create(IssueCategory.PackageVersion, isPreview ? k_RecommendPackagePreView : k_RecommendPackageUpgrade, package.displayName)
                    .WithCustomProperties(new object[(int)PackageVersionProperty.Num]
                    {
                        package.name,
                        package.version,
                        recommendedVersionString,
                        isPreview
                    });
                issues.Add(packageVersionIssue);
            }
        }
    }
}
