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
    enum AudioClipProperty
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

        internal override string name => "AudioClip";

        internal override bool isEnabledByDefault => false;

        internal override IReadOnlyCollection<IssueLayout> supportedLayouts => new IssueLayout[]
        {
            k_AudioClipLayout,
            AssetsModule.k_IssueLayout
        };

        internal override void Audit(ProjectAuditorParams projectAuditorParams, IProgress progress = null)
        {
            projectAuditorParams.onIncomingIssues(EnumerateAudioClips(projectAuditorParams.platform));
            projectAuditorParams.onModuleCompleted?.Invoke();
        }

        IEnumerable<ProjectIssue> EnumerateAudioClips(BuildTarget platform)
        {
            var GUIDsAudioClip = AssetDatabase.FindAssets("t:AudioClip");
            foreach (var guid in GUIDsAudioClip)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var importer = AssetImporter.GetAtPath(path) as AudioImporter;
                var sampleSettings = importer.GetOverrideSampleSettings(platform.ToString());
                yield return ProjectIssue.Create(IssueCategory.AudioClip, Path.GetFileNameWithoutExtension(path)).WithCustomProperties(new object[(int)AudioClipProperty.Num]
                {
                    importer.forceToMono,
                    importer.loadInBackground,
#if UNITY_2022_2_OR_NEWER
                    sampleSettings.preloadAudioData,
#else
                    importer.preloadAudioData,
#endif
                    sampleSettings.loadType,
                    sampleSettings.compressionFormat
                }).WithLocation(path);
            }
        }
    }
}
