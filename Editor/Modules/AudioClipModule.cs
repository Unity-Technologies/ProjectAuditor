using System.Collections;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using System.Linq;
using UnityEditor;
using System.IO;
using Unity.ProjectAuditor.Editor.Core;

namespace Unity.ProjectAuditor.Editor.Modules
{
    public enum AudioClipProperty
    {
        ForceToMono = 0,
        LoadInBackground,
        PreloadAudioData,
        LoadType,
        CompressionFormat,
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
                new PropertyDefinition { type = PropertyType.FileType, name = "Format", defaultGroup = true },
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(AudioClipProperty.ForceToMono), format = PropertyFormat.Bool, name = "Force To Mono"},
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(AudioClipProperty.LoadInBackground), format = PropertyFormat.Bool, name = "Load In Background"},
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(AudioClipProperty.PreloadAudioData), format = PropertyFormat.Bool, name = "Preload Audio Data" },
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(AudioClipProperty.LoadType), format = PropertyFormat.String, name = "Load Type" },
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(AudioClipProperty.CompressionFormat), format = PropertyFormat.String, name = "Compression Format"},
                new PropertyDefinition { type = PropertyType.Path, name = "Path"}
            }
        };

        public override string name => "AudioClip";

        public override IReadOnlyCollection<IssueLayout> supportedLayouts => new IssueLayout[]
        {
            k_AudioClipLayout,
        };

        public override void Audit(ProjectAuditorParams projectAuditorParams, IProgress progress = null)
        {
            var issues = new List<ProjectIssue>();
            AnalyzeAudioClip(issues, projectAuditorParams.platform);
            if (issues.Count > 0)
                projectAuditorParams.onIncomingIssues(issues);
            projectAuditorParams.onModuleCompleted?.Invoke();
        }

        private void AnalyzeAudioClip(List<ProjectIssue> issues, BuildTarget platform)
        {
            var GUIDsAudioClip = AssetDatabase.FindAssets("t:AudioClip");
            foreach (var guid in GUIDsAudioClip)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var importer = AssetImporter.GetAtPath(path) as AudioImporter;
                var audioClipIssue = ProjectIssue.Create(IssueCategory.AudioClip, Path.GetFileNameWithoutExtension(path)).WithCustomProperties(new object[(int)AudioClipProperty.Num]
                {
                    importer.forceToMono,
                    importer.loadInBackground,
#if UNITY_2022_2_OR_NEWER
                    importer.GetOverrideSampleSettings(platform.ToString()).preloadAudioData,
#else
                    importer.preloadAudioData,
#endif
                    importer.GetOverrideSampleSettings(platform.ToString()).loadType,
                    importer.GetOverrideSampleSettings(platform.ToString()).compressionFormat
                }).WithLocation(path);
                issues.Add(audioClipIssue);
            }
        }

        public override bool isEnabledByDefault => false;
    }
}
