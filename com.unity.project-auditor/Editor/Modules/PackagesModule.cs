using System.Collections.Generic;
using UnityEditor.PackageManager;
using System.Linq;
using Unity.ProjectAuditor.Editor.Core;
using Unity.ProjectAuditor.Editor.Diagnostic;
using Unity.ProjectAuditor.Editor.Utils;

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
            Category = IssueCategory.Package,
            Properties = new[]
            {
                new PropertyDefinition { Type = PropertyType.Description, Name = "Package" },
                new PropertyDefinition { Type = PropertyTypeUtil.FromCustom(PackageProperty.Name), Format = PropertyFormat.String, Name = "Name", IsHidden = true },
                new PropertyDefinition { Type = PropertyTypeUtil.FromCustom(PackageProperty.Version), Format = PropertyFormat.String, Name = "Version" },
                new PropertyDefinition { Type = PropertyTypeUtil.FromCustom(PackageProperty.Source), Format = PropertyFormat.String, Name = "Source", IsDefaultGroup = true },
                new PropertyDefinition { Type = PropertyType.Path, Format = PropertyFormat.String, Name = "Path" }
            }
        };

        internal const string PAP0001 = nameof(PAP0001);
        internal const string PAP0002 = nameof(PAP0002);

        static readonly Descriptor k_RecommendPackageUpgrade = new Descriptor(
            PAP0001,
            "Newer recommended package version",
            Areas.Quality,
            "A newer recommended version of this package is available.",
            "Update the package via Package Manager."
        )
        {
            MessageFormat = "Package '{0}' could be updated from version '{1}' to '{2}'",
            DefaultSeverity = Severity.Minor
        };

        static readonly Descriptor k_RecommendPackagePreView = new Descriptor(
            PAP0002,
            "Experimental/Preview packages",
            Areas.Quality,
            "Experimental or Preview packages are in the early stages of development and not yet ready for production.",
            "Experimental packages should only be used for testing purposes and to give feedback to Unity."
        )
        {
            MessageFormat = "Package '{0}' version '{1}' is a preview/experimental version"
        };

        public override string Name => "Packages";

        public override IReadOnlyCollection<IssueLayout> SupportedLayouts => new IssueLayout[]
        {
            k_PackageLayout,
            SettingsModule.k_IssueLayout
        };

        public override void Initialize()
        {
            base.Initialize();

            RegisterDescriptor(k_RecommendPackageUpgrade);
            RegisterDescriptor(k_RecommendPackagePreView);
        }

        public override AnalysisResult Audit(AnalysisParams analysisParams, IProgress progress = null)
        {
            var request = Client.List();
            while (!request.IsCompleted)
                System.Threading.Thread.Sleep(10);
            if (request.Status == StatusCode.Failure)
            {
                return AnalysisResult.Failure;
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
            return AnalysisResult.Success;
        }

        IEnumerable<ProjectIssue> EnumerateInstalledPackages(AnalysisContext context, UnityEditor.PackageManager.PackageInfo package)
        {
            var dependencies = package.dependencies.Select(d => d.name + " [" + d.version + "]").ToArray();
            var displayName = string.IsNullOrEmpty(package.displayName) ? package.name : package.displayName;
            var node = new PackageDependencyNode(displayName, dependencies);
            yield return context.CreateInsight(IssueCategory.Package, displayName)
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
            // first check if any package is preview or experimental
            if (package.version.Contains("pre") || package.version.Contains("exp"))
            {
                yield return context.CreateIssue(IssueCategory.ProjectSetting, k_RecommendPackagePreView.Id, package.name, package.version)
                    .WithLocation(package.assetPath);
            }
            else
            {
                // if not preview or experimental, check anyway if there is a recommended version available
                var recommendedVersionString = PackageUtils.GetPackageRecommendedVersion(package);
                if (!string.IsNullOrEmpty(package.version) && !string.IsNullOrEmpty(recommendedVersionString))
                {
                    if (!recommendedVersionString.Equals(package.version))
                    {
                        yield return context.CreateIssue(IssueCategory.ProjectSetting, k_RecommendPackageUpgrade.Id, package.name, package.version, recommendedVersionString)
                            .WithLocation(package.assetPath);
                    }
                }
            }
        }
    }
}
