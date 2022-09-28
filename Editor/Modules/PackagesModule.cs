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
                new PropertyDefinition { type = PropertyType.Description, name = "Issue", longName = "Package Issue"},
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(PackageVersionProperty.CurrentVersion), format = PropertyFormat.String, name = "Current Version" },
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(PackageVersionProperty.RecommendedVersion), format = PropertyFormat.String, name = "Recommended Version"},
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(PackageVersionProperty.Experimental), format = PropertyFormat.Bool, name = "Experimental/Preview" },
                new PropertyDefinition { type = PropertyType.Descriptor, name = "Descriptor", defaultGroup = true},
            }
        };


        static readonly ProblemDescriptor k_RecommendPackageUpgrade  = new ProblemDescriptor(
            "PAP0001",
            "Upgradable packages",
            new[] { Area.Quality },
            "A newer recommended version of this package is available.",
            "Upgrade the package via Package Manager."
        )
        {
            messageFormat = "'{0}' is not up to date",
        };


        static readonly ProblemDescriptor k_RecommendPackagePreView = new ProblemDescriptor(
            "PAP0002",
            "Experimental/Preview packages",
            new[] { Area.Quality },
            "Experimental or Preview packages are in the early stages of development and not yet ready for production.",
            "We recommend using these only for testing purposes and to give us direct feedback"
        )
        {
            messageFormat = "'{0}' is in preview/experimental mode"
        };

        public override string name => "Packages";

        public override IReadOnlyCollection<IssueLayout> supportedLayouts => new IssueLayout[]
        {
            k_PackageLayout,
            k_PackageVersionLayout
        };

        public override void Audit(ProjectAuditorParams projectAuditorParams, IProgress progress = null)
        {
            var request = Client.List();
            while (!request.IsCompleted)
                System.Threading.Thread.Sleep(10);
            if (request.Status == StatusCode.Failure)
            {
                projectAuditorParams.onModuleCompleted?.Invoke();
                return;
            }
            var issues = new List<ProjectIssue>();
            foreach (var package in request.Result)
            {
                issues.AddRange(EnumerateInstalledPackages(package));
                issues.AddRange(EnumeratePackageDiagnostics(package));
            }
            if (issues.Count > 0)
                projectAuditorParams.onIncomingIssues(issues);
            projectAuditorParams.onModuleCompleted?.Invoke();
        }

        IEnumerable<ProjectIssue> EnumerateInstalledPackages(UnityEditor.PackageManager.PackageInfo package)
        {
            var dependencies = package.dependencies.Select(d => d.name + " [" + d.version + "]").ToArray();
            var node = new PackageDependencyNode(package.displayName, dependencies);
            yield return ProjectIssue.Create(IssueCategory.Package, package.displayName)
                .WithCustomProperties(new object[(int)PackageProperty.Num]
                {
                    package.name,
                    package.version,
                    package.source
                })
                .WithDependencies(node);
        }

        IEnumerable<ProjectIssue> EnumeratePackageDiagnostics(UnityEditor.PackageManager.PackageInfo package)
        {
            var recommendedVersionString = PackageUtils.GetPackageRecommendedVersion(package);
            if (!string.IsNullOrEmpty(package.version) && !string.IsNullOrEmpty(recommendedVersionString))
            {
                if (!recommendedVersionString.Equals(package.version))
                {
                    yield return ProjectIssue.Create(IssueCategory.PackageVersion, k_RecommendPackageUpgrade, package.name)
                        .WithCustomProperties(new object[(int)PackageVersionProperty.Num]
                        {
                            package.name,
                            package.version,
                            recommendedVersionString,
                            false
                        });
                }
            }
            else if (package.version.Contains("pre") || package.version.Contains("exp"))
            {
                yield return ProjectIssue.Create(IssueCategory.PackageVersion, k_RecommendPackagePreView, package.name)
                    .WithCustomProperties(new object[(int)PackageVersionProperty.Num]
                    {
                        package.name,
                        package.version,
                        recommendedVersionString,
                        true
                    });
            }
        }
    }
}
