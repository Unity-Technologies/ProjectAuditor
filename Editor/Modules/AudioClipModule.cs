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
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(AudioClipProperty.ForceToMono), format = PropertyFormat.Bool, name = "Force To Mono"},
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(AudioClipProperty.LoadInBackground), format = PropertyFormat.Bool, name = "Load In Background"},
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(AudioClipProperty.PreloadAudioData), format = PropertyFormat.Bool, name = "Preload AudioData" },
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(AudioClipProperty.LoadType), format = PropertyFormat.String, name = "Load Type" },
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(AudioClipProperty.CompressionFormat), format = PropertyFormat.String, name = "Compression Format"},
                new PropertyDefinition { type = PropertyType.Path, name = "Path"}
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
                }).WithLocation(path);
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
