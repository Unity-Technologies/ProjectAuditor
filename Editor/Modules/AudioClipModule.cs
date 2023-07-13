using System.Collections;
using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using System.Linq;
using UnityEditor;
using System.IO;
using System.Reflection;
using Unity.ProjectAuditor.Editor.Core;
using UnityEngine.Profiling;

namespace Unity.ProjectAuditor.Editor.Modules
{
    enum AudioClipProperty
    {
        Length = 0,
        SourceFileSize,
        ImportedFileSize,
        RuntimeSize,
        CompressionRatio,
        CompressionFormat,
        SampleRate,
        ForceToMono,
        LoadInBackground,
        PreloadAudioData,
        LoadType,

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
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(AudioClipProperty.Length), format = PropertyFormat.String, name = "Length"},
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(AudioClipProperty.SourceFileSize), format = PropertyFormat.Bytes, name = "Source File Size"},
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(AudioClipProperty.ImportedFileSize), format = PropertyFormat.Bytes, name = "Imported File Size"},
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(AudioClipProperty.RuntimeSize), format = PropertyFormat.Bytes, name = "Runtime Size"},
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(AudioClipProperty.CompressionRatio), format = PropertyFormat.String, name = "Compression Ratio"},
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(AudioClipProperty.CompressionFormat), format = PropertyFormat.String, name = "Compression Format"},
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(AudioClipProperty.SampleRate), format = PropertyFormat.String, name = "Sample Rate"},
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(AudioClipProperty.ForceToMono), format = PropertyFormat.Bool, name = "Force To Mono"},
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(AudioClipProperty.LoadInBackground), format = PropertyFormat.Bool, name = "Load In Background"},
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(AudioClipProperty.PreloadAudioData), format = PropertyFormat.Bool, name = "Preload Audio Data" },
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(AudioClipProperty.LoadType), format = PropertyFormat.String, name = "Load Type" },
                new PropertyDefinition { type = PropertyType.Path, name = "Path"}
            }
        };

        public override string name => "AudioClips";

        public override bool isEnabledByDefault => false;

        public override IReadOnlyCollection<IssueLayout> supportedLayouts => new IssueLayout[]
        {
            k_AudioClipLayout,
            AssetsModule.k_IssueLayout
        };

        public override void Audit(ProjectAuditorParams projectAuditorParams, IProgress progress = null)
        {
            projectAuditorParams.onIncomingIssues(EnumerateAudioClips(projectAuditorParams.platform));
            projectAuditorParams.onModuleCompleted?.Invoke();
        }

        public static object GetPropertyValue(AssetImporter assetImporter, string propertyName)
        {
            Type objType = assetImporter.GetType();
            PropertyInfo propInfo = objType.GetProperty(propertyName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            if (propInfo == null)
                throw new ArgumentOutOfRangeException("propertyName",
                    string.Format("Couldn't find property {0} in type {1}", propertyName, objType.FullName));
            return propInfo.GetValue(assetImporter, null);
        }

        IEnumerable<ProjectIssue> EnumerateAudioClips(BuildTarget platform)
        {
            var GUIDsAudioClip = AssetDatabase.FindAssets("t:AudioClip");
            foreach (var guid in GUIDsAudioClip)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var importer = AssetImporter.GetAtPath(path) as AudioImporter;
                var sampleSettings = importer.GetOverrideSampleSettings(platform.ToString());
                // SteveM TODO: The analyzer will want this to avoid having to reload it, so make sure you can pass it in
                var audioClip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);

                // TODO: the size returned by the profiler is not the exact size on the target platform. Needs to be fixed.
                var runtimeSize = Profiler.GetRuntimeMemorySizeLong(audioClip);
                var origSize = (int)GetPropertyValue(importer, "origSize");
                var compSize = (int)GetPropertyValue(importer, "compSize");

                var ts = new TimeSpan(0, 0, 0, 0, (int)(audioClip.length * 1000.0f));

                yield return ProjectIssue.Create(IssueCategory.AudioClip, Path.GetFileNameWithoutExtension(path)).WithCustomProperties(new object[(int)AudioClipProperty.Num]
                {
                    String.Format("{0:00}:{1:00}.{2:000}", ts.Minutes, ts.Seconds, ts.Milliseconds),
                    origSize,
                    compSize,
                    runtimeSize,
                    (100.0f * (float)compSize / (float)origSize).ToString("0.00", CultureInfo.InvariantCulture.NumberFormat) + "%",
                    sampleSettings.compressionFormat,
                    ((float)audioClip.frequency / 1000.0f).ToString("G0", CultureInfo.InvariantCulture.NumberFormat) + " KHz",
                    importer.forceToMono,
                    importer.loadInBackground,
#if UNITY_2022_2_OR_NEWER
                    sampleSettings.preloadAudioData,
#else
                    importer.preloadAudioData,
#endif
                    sampleSettings.loadType,

                }).WithLocation(path);
            }
        }
    }
}
