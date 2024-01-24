using System.Collections.Generic;
using UnityEditor.PackageManager;
using System.Linq;
using Unity.ProjectAuditor.Editor.Core;
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

    class PackagesModule : ModuleWithAnalyzers<PackagesModuleAnalyzer>
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

        public override string Name => "Packages";

        public override IReadOnlyCollection<IssueLayout> SupportedLayouts => new IssueLayout[]
        {
            k_PackageLayout,
            SettingsModule.k_IssueLayout
        };

        public override AnalysisResult Audit(AnalysisParams analysisParams, IProgress progress = null)
        {
            var analyzers = GetCompatibleAnalyzers(analysisParams);
            if (analyzers.Length == 0)
                return AnalysisResult.Success;

            var packages = PackageUtils.GetClientPackages();
            var packageCount = packages.Length;

            progress?.Start("Finding Packages", "Search in Progress...", packageCount);

            var context = new PackageAnalysisContext
            {
                Params = analysisParams
            };

            foreach (var package in packages)
            {
                if (progress?.IsCancelled ?? false)
                    return AnalysisResult.Cancelled;

                progress?.Advance(package.displayName);

                context.PackageInfo = package;

                analysisParams.OnIncomingIssues(EnumerateInstalledPackages(context));

                foreach (var analyzer in analyzers)
                {
                    analysisParams.OnIncomingIssues(analyzer.Analyze(context));
                }
            }

            progress?.Clear();
            return AnalysisResult.Success;
        }

        IEnumerable<ReportItem> EnumerateInstalledPackages(PackageAnalysisContext context)
        {
            var package = context.PackageInfo;
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
    }
}
