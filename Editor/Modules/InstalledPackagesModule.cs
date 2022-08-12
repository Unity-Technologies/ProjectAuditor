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
        Name,
        Version,
        Source,
        Num
    }

    class InstalledPackagesModule : ProjectAuditorModule
    {
        static ListRequest request;

        static readonly IssueLayout k_packagesLayout = new IssueLayout
        {
            category = IssueCategory.installedPackages,
            properties = new[]
            {
                new PropertyDefinition { type = PropertyType.Description, name = "Display Name", },
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(PackageProperty.Name), format = PropertyFormat.String, name = "Name" },
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(PackageProperty.Version), format = PropertyFormat.String, name = "version" },
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(PackageProperty.Source), format = PropertyFormat.String, name = "source" }
            }
        };
        public override void Audit(ProjectAuditorParams projectAuditorParams, IProgress progress = null)
        {
            progress?.Start("Analyzing packages", "Anaylyzing installed packages", int.MaxValue);
            request = Client.List();
            progress?.Advance();
            while (!(request.Status == StatusCode.Success)) {}
            var issues = new List<ProjectIssue>();
            foreach (var package in request.Result)
            {
                AddInstalledPackage(package, issues.Add);
            }
            if (issues.Count > 0)
                projectAuditorParams.onIncomingIssues(issues);
            progress?.Clear();
            projectAuditorParams.onModuleCompleted?.Invoke();
        }

        void AddInstalledPackage(UnityEditor.PackageManager.PackageInfo package, Action<ProjectIssue> issueFound)
        {
            string[] dependecies = package.dependencies.Select(d => d.name + " [" + d.version + "]").ToArray();
            PackageDependencyNode testNode = new PackageDependencyNode(package.displayName, dependecies);
            var packageIssue = ProjectIssue.Create(IssueCategory.installedPackages, package.displayName).WithCustomProperties(new object[(int)PackageProperty.Num]
            {
                package.name,
                package.version,
                package.source
            }).WithDependencies(testNode);
            issueFound(packageIssue);
        }

        public override IEnumerable<IssueLayout> GetLayouts()
        {
            yield return k_packagesLayout;
        }

        public override bool IsEnabledByDefault()
        {
            return true;
        }
    }
}
