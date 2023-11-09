using System;
using System.Collections.Generic;
using UnityEditor.PackageManager;
using System.Linq;
using Unity.ProjectAuditor.Editor.Core;
using Unity.ProjectAuditor.Editor.Diagnostic;
using Unity.ProjectAuditor.Editor.Utils;
using UnityEditor;
using UnityEngine;


namespace Unity.ProjectAuditor.Editor.Modules
{
    enum PackageProperty
    {
        Name = 0,
        Version,
        Source,
        Num
    }

    class PackagesModule : Module
    {
        static readonly IssueLayout k_PackageLayout = new IssueLayout
        {
            category = IssueCategory.Package,
            properties = new[]
            {
                new PropertyDefinition { type = PropertyType.Description, name = "Package" },
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(PackageProperty.Name), format = PropertyFormat.String, name = "Name", hidden = true },
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(PackageProperty.Version), format = PropertyFormat.String, name = "Version" },
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(PackageProperty.Source), format = PropertyFormat.String, name = "Source", defaultGroup = true },
                new PropertyDefinition { type = PropertyType.Path, format = PropertyFormat.String, name = "Path" }
            }
        };

        static readonly IssueLayout k_PackageVersionLayout = new IssueLayout
        {
            category = IssueCategory.PackageDiagnostic,
            properties = new[]
            {
                new PropertyDefinition { type = PropertyType.Description, name = "Issue", longName = "Package Issue"},
                new PropertyDefinition { type = PropertyType.Severity, format = PropertyFormat.String, name = "Severity"},
                new PropertyDefinition { type = PropertyType.Descriptor, name = "Descriptor", defaultGroup = true, hidden = true},
            }
        };

        internal const string PAP0001 = nameof(PAP0001);
        internal const string PAP0002 = nameof(PAP0002);

        static readonly Descriptor k_RecommendPackageUpgrade = new Descriptor(
            PAP0001,
            "Newer recommended package version",
            new[] { Area.Quality },
            "A newer recommended version of this package is available.",
            "Update the package via Package Manager."
        )
        {
            messageFormat = "'{0}' could be updated from version '{1}' to '{2}'",
            defaultSeverity = Severity.Minor
        };

        static readonly Descriptor k_RecommendPackagePreView = new Descriptor(
            PAP0002,
            "Experimental/Preview packages",
            new[] { Area.Quality },
            "Experimental or Preview packages are in the early stages of development and not yet ready for production.",
            "Experimental packages should only be used for testing purposes and to give feedback to Unity."
        )
        {
            messageFormat = "'{0}' version '{1}' is a preview/experimental version"
        };

        public override string Name => "Packages";

        public override IReadOnlyCollection<IssueLayout> SupportedLayouts => new IssueLayout[]
        {
            k_PackageLayout,
            k_PackageVersionLayout
        };

        public override void Initialize()
        {
            base.Initialize();

            RegisterDescriptor(k_RecommendPackageUpgrade);
            RegisterDescriptor(k_RecommendPackagePreView);
        }

        public override void Audit(AnalysisParams analysisParams, IProgress progress = null)
        {
            var request = Client.List();
            while (!request.IsCompleted)
                System.Threading.Thread.Sleep(10);
            if (request.Status == StatusCode.Failure)
            {
                analysisParams.OnModuleCompleted?.Invoke();
                return;
            }

            var context = new AnalysisContext()
            {
                Params = analysisParams
            };

            foreach (var package in request.Result)
            {
                analysisParams.OnIncomingIssues(EnumerateInstalledPackages(context, package));
                analysisParams.OnIncomingIssues(EnumeratePackageDiagnostics(context, package));
            }
            analysisParams.OnModuleCompleted?.Invoke();
        }

        IEnumerable<ProjectIssue> EnumerateInstalledPackages(AnalysisContext context, UnityEditor.PackageManager.PackageInfo package)
        {
            var dependencies = package.dependencies.Select(d => d.name + " [" + d.version + "]").ToArray();
            var displayName = string.IsNullOrEmpty(package.displayName) ? package.name : package.displayName;
            var node = new PackageDependencyNode(displayName, dependencies);
            yield return context.CreateWithoutDiagnostic(IssueCategory.Package, displayName)
                .WithCustomProperties(new object[(int)PackageProperty.Num]
                {
                    package.name,
                    package.version,
                    package.source
                })
                .WithDependencies(node)
                .WithLocation(package.assetPath);
        }

        IEnumerable<ProjectIssue> EnumeratePackageDiagnostics(AnalysisContext context, UnityEditor.PackageManager.PackageInfo package)
        {
            var recommendedVersionString = PackageUtils.GetPackageRecommendedVersion(package);
            if (!string.IsNullOrEmpty(package.version) && !string.IsNullOrEmpty(recommendedVersionString))
            {
                if (!recommendedVersionString.Equals(package.version))
                {
                    yield return context.Create(IssueCategory.PackageDiagnostic, k_RecommendPackageUpgrade.id, package.name, package.version, recommendedVersionString)
                        .WithLocation(package.assetPath);
                }
            }
            else if (package.version.Contains("pre") || package.version.Contains("exp"))
            {
                yield return context.Create(IssueCategory.PackageDiagnostic, k_RecommendPackagePreView.id, package.name, package.version)
                    .WithLocation(package.assetPath);
            }
        }
    }
}
