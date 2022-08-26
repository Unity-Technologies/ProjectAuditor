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
    public enum AudioProperty
    {
        Name = 0,
        Format,             //AudioImporter
        ForceToMono,        //AudioImporter
        DecompressOnLoad,   //AudioImporter
        LoadInBackground,   //AudioImporter
        PreloadAudioData,   //AudioImporter
        LoadType,           //AudioImporterSampleSettings
        CompressionFormat,  //AudioImporterSampleSettings
        Num
    }

    class AudioClipModule : ProjectAuditorModule
    {
        static readonly IssueLayout k_AudioClipLayout = new IssueLayout
        {
            category = IssueCategory.AudioClip,
            properties = new[]
            {
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(AudioProperty.Name), format = PropertyFormat.String, name = "Name" },
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(AudioProperty.Format), format = PropertyFormat.String, name = "Format", defaultGroup = true },
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(AudioProperty.ForceToMono), format = PropertyFormat.String, name = "ForceToMono"},
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(AudioProperty.DecompressOnLoad), format = PropertyFormat.String, name = "DecompressOnLoad" },
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(AudioProperty.LoadInBackground), format = PropertyFormat.String, name = "LoadInBackground"},
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(AudioProperty.PreloadAudioData), format = PropertyFormat.String, name = "PreloadAudioData" },
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(AudioProperty.LoadType), format = PropertyFormat.String, name = "LoadType" },
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(AudioProperty.CompressionFormat), format = PropertyFormat.String, name = "CompressionFormat"}
            }
        };
        public override void Audit(ProjectAuditorParams projectAuditorParams, IProgress progress = null)
        {
            var issues = new List<ProjectIssue>();
            AnalyzeAudioClip(issues);
            if (issues.Count > 0)
                projectAuditorParams.onIncomingIssues(issues);
            projectAuditorParams.onModuleCompleted?.Invoke();
        }

        private void AnalyzeAudioClip(List<ProjectIssue> issues)
        {
            UnityEngine.Object[] objects = Selection.GetFiltered(typeof(AudioClip), SelectionMode.DeepAssets);
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
            yield return k_AudioClipLayout;
        }

        public override bool IsEnabledByDefault()
        {
            return false;
        }
    }
}
