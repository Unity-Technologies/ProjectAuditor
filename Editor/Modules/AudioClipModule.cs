using System.Collections;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using System.Linq;
using UnityEditor;
using System.IO;


namespace Unity.ProjectAuditor.Editor.Modules
{
    public enum AudioClipProperty
    {
        Format,             //AudioImporter
        ForceToMono,        //AudioImporter
        LoadInBackground,   //AudioImporter
        PreloadAudioData,   //AudioImporter
        LoadType,           //AudioImporter.defaultSampleSettings
        CompressionFormat,  //AudioImporter.defaultSampleSettings
        Num
    }

    class AudioClipModule : ProjectAuditorModule
    {
        static readonly IssueLayout k_AudioClipLayout = new IssueLayout
        {
            category = IssueCategory.AudioClip,
            properties = new[]
            {
                new PropertyDefinition { type = PropertyType.Description, name = "Name" },
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(AudioClipProperty.Format), format = PropertyFormat.String, name = "Format", defaultGroup = true },
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(AudioClipProperty.ForceToMono), format = PropertyFormat.Bool, name = "ForceToMono"},
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(AudioClipProperty.LoadInBackground), format = PropertyFormat.Bool, name = "LoadInBackground"},
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(AudioClipProperty.PreloadAudioData), format = PropertyFormat.Bool, name = "PreloadAudioData" },
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(AudioClipProperty.LoadType), format = PropertyFormat.String, name = "LoadType" },
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(AudioClipProperty.CompressionFormat), format = PropertyFormat.String, name = "CompressionFormat"}
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
            var GUIDsAudioClip = AssetDatabase.FindAssets("t:AudioClip");
            foreach (var guid in GUIDsAudioClip)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var importer = AssetImporter.GetAtPath(path) as AudioImporter;
                var audioClipIssue = ProjectIssue.Create(IssueCategory.AudioClip, Path.GetFileNameWithoutExtension(path)).WithCustomProperties(new object[(int)AudioClipProperty.Num]
                {
                    Path.GetExtension(path).Substring(1),
                    importer.forceToMono,
                    importer.loadInBackground,
                    importer.preloadAudioData,
                    importer.defaultSampleSettings.loadType,
                    importer.defaultSampleSettings.compressionFormat
                });
                issues.Add(audioClipIssue);
            }
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
