using System.Collections;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using System.Linq;
using Unity.ProjectAuditor.Editor.Core;
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
        public override void Audit(ProjectAuditorParams projectAuditorParams, IProgress progress = null)
        {
            var request = Client.List();
            while (request.Status != StatusCode.Success) {}
            var issues = new List<ProjectIssue>();
            foreach (var package in request.Result)
            {
                AddInstalledPackage(package, issues);
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

        public override IEnumerable<IssueLayout> GetLayouts()
        {
            yield return k_PackageLayout;
        }
    }
}
