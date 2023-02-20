using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.ProjectAuditor.Editor.Core;
using Unity.ProjectAuditor.Editor.Diagnostic;
using Unity.ProjectAuditor.Editor.Modules;
using UnityEditor;
using UnityEngine;

namespace Unity.ProjectAuditor.Editor.SettingsAnalysis
{
    class QualitySettingsAnalyzer : ISettingsModuleAnalyzer
    {
        internal const string PAS0018 = nameof(PAS0018);
        internal const string PAS0019 = nameof(PAS0019);
        internal const string PAS0020 = nameof(PAS0020);
        internal const string PAS0021 = nameof(PAS0021);
        internal const string PAS1007 = nameof(PAS1007);

        static readonly Descriptor k_DefaultSettingsDescriptor = new Descriptor(
            PAS0018,
            "Quality: Quality Levels",
            new[] { Area.CPU, Area.GPU, Area.BuildSize, Area.LoadTime },
            "This project is using the default set of quality levels defined in <b>Project Settings ➔ Quality</b>.",
            "Check the quality setting for each platform the project supports in the grid - it's the level with the green tick. Remove quality levels you are not using, to make the Quality Settings simpler to see and edit. Adjust the setting for each platform if necessary, then select the appropriate levels to examine their settings in the panel below.");

        static readonly Descriptor k_UsingLowQualityTexturesDescriptor = new Descriptor(
            PAS0019,
            "Quality: Texture Quality",
            new[] { Area.GPU, Area.BuildSize },
            "One or more of the quality levels in the project's Quality Settings has <b>Texture Quality</b> set to something other than Full Res. This option can save memory on lower-spec devices and platforms by discarding higher-resolution mip levels on mipmapped textures before uploading them to the GPU. However, this option has no effect on textures which don't have mipmaps enabled (as is frequently the case with UI textures, for instance), does nothing to reduce download or install size, and gives you no control over the texture resize algorithm.",
            "For devices which must use lower-resolution versions of textures, consider creating these lower resolution textures separately, and choosing the appropriate content to load at runtime using AssetBundle variants.");

        static readonly Descriptor k_DefaultAsyncUploadTimeSliceDescriptor = new Descriptor(
            PAS0020,
            "Quality: Async Upload Time Slice",
            Area.LoadTime,
            "The <b>Async Upload Time Slice</b> option for one or more quality levels in the project's Quality Settings is set to the default value of 2ms.",
            "If the project encounters long loading times when loading large amount of texture and/or mesh data, experiment with increasing this value to see if it allows content to be uploaded to the GPU more quickly.");

        static readonly Descriptor k_DefaultAsyncUploadBufferSizeSliceDescriptor = new Descriptor(
            PAS0021,
            "Quality: Async Upload Buffer Size",
            Area.LoadTime,
            "The <b>Async Upload Buffer Size</b> option for one or more quality levels in the project's Quality Settings is set to the default value.",
            "If the project encounters long loading times when loading large amount of texture and/or mesh data, experiment with increasing this value to see if it allows content to be uploaded to the GPU more quickly. This is most likely to help if you are loading large textures. Note that this setting controls a buffer size in megabytes, so exercise caution if memory is limited in your application.");

        static readonly Descriptor k_TextureStreamingDisabledDescriptor = new Descriptor(
            PAS1007,
            "Quality: Texture streaming disabled",
            Area.Quality,
            "<b>Texture Streaming </b> is disabled. More mipmap textures will be loaded into memory on the GPU.",
            "If your project contains many high resolution textures, enable Texture Streaming in <b>Project Settings ➔ Quality</b> ")
            {
                fixer = (issue =>
                {
                    EnableStreamingMipmap(issue.GetCustomPropertyInt32(0));
                }),

                messageFormat = "Settings: Texture streaming on Quality Level '{0}' is turned off."

            };

        public void Initialize(ProjectAuditorModule module)
        {
            module.RegisterDescriptor(k_DefaultSettingsDescriptor);
            module.RegisterDescriptor(k_UsingLowQualityTexturesDescriptor);
            module.RegisterDescriptor(k_DefaultAsyncUploadTimeSliceDescriptor);
            module.RegisterDescriptor(k_DefaultAsyncUploadBufferSizeSliceDescriptor);
        }

        public IEnumerable<ProjectIssue> Analyze(ProjectAuditorParams projectAuditorParams)
        {
            if (IsUsingDefaultSettings())
            {
                yield return ProjectIssue.Create(IssueCategory.ProjectSetting, k_DefaultSettingsDescriptor)
                    .WithLocation("Project/Quality");
            }

            if (IsUsingLowQualityTextures())
            {
                yield return ProjectIssue.Create(IssueCategory.ProjectSetting, k_UsingLowQualityTexturesDescriptor)
                    .WithLocation("Project/Quality");
            }

            if (IsDefaultAsyncUploadTimeSlice())
            {
                yield return ProjectIssue.Create(IssueCategory.ProjectSetting, k_DefaultAsyncUploadTimeSliceDescriptor)
                    .WithLocation("Project/Quality");
            }

            if (IsDefaultAsyncUploadBufferSize())
            {
                yield return ProjectIssue.Create(IssueCategory.ProjectSetting, k_DefaultAsyncUploadBufferSizeSliceDescriptor)
                    .WithLocation("Project/Quality");
            }

            if (GetTextureStreamingDisabledQualityLevelsIndex().Count != 0)
            {
                var qualityLevels = GetTextureStreamingDisabledQualityLevelsIndex();
                for (int i = 0; i < qualityLevels.Count; i++)
                {
                    yield return ProjectIssue.Create(IssueCategory.ProjectSetting, k_TextureStreamingDisabledDescriptor, QualitySettings.names[qualityLevels[i]]).
                        WithCustomProperties(new object[]{qualityLevels[i]}).WithLocation("Project/Quality");
                }
            }
        }

        internal static bool IsUsingDefaultSettings()
        {
            return (QualitySettings.names.Length == 6 &&
                QualitySettings.names[0] == "Very Low" &&
                QualitySettings.names[1] == "Low" &&
                QualitySettings.names[2] == "Medium" &&
                QualitySettings.names[3] == "High" &&
                QualitySettings.names[4] == "Very High" &&
                QualitySettings.names[5] == "Ultra");
        }

        internal static bool IsUsingLowQualityTextures()
        {
            var usingLowTextureQuality = false;
            var initialQualityLevel = QualitySettings.GetQualityLevel();

            for (var i = 0; i < QualitySettings.names.Length; ++i)
            {
                QualitySettings.SetQualityLevel(i);

#if UNITY_2022_2_OR_NEWER
                if (QualitySettings.globalTextureMipmapLimit > 0)
#else
                if (QualitySettings.masterTextureLimit > 0)
#endif
                {
                    usingLowTextureQuality = true;
                    break;
                }
            }

            QualitySettings.SetQualityLevel(initialQualityLevel);
            return usingLowTextureQuality;
        }

        internal static bool IsDefaultAsyncUploadTimeSlice()
        {
            var usingDefaultAsyncUploadTimeslice = false;
            var initialQualityLevel = QualitySettings.GetQualityLevel();

            for (var i = 0; i < QualitySettings.names.Length; ++i)
            {
                QualitySettings.SetQualityLevel(i);

                if (QualitySettings.asyncUploadTimeSlice == 2)
                {
                    usingDefaultAsyncUploadTimeslice = true;
                    break;
                }
            }

            QualitySettings.SetQualityLevel(initialQualityLevel);
            return usingDefaultAsyncUploadTimeslice;
        }

        internal static bool IsDefaultAsyncUploadBufferSize()
        {
            var usingDefaultAsyncUploadBufferSize = false;
            var initialQualityLevel = QualitySettings.GetQualityLevel();

            for (var i = 0; i < QualitySettings.names.Length; ++i)
            {
                QualitySettings.SetQualityLevel(i);

                if (QualitySettings.asyncUploadBufferSize == 4 || QualitySettings.asyncUploadBufferSize == 16)
                {
                    usingDefaultAsyncUploadBufferSize = true;
                    break;
                }
            }

            QualitySettings.SetQualityLevel(initialQualityLevel);
            return usingDefaultAsyncUploadBufferSize;
        }

        internal static List<int> GetTextureStreamingDisabledQualityLevelsIndex()
        {
            var initialQualityLevel = QualitySettings.GetQualityLevel();
            var qualityIndexes = new List<int>();

            for (var i = 0; i < QualitySettings.names.Length; i++)
            {
                QualitySettings.SetQualityLevel(i);

                if (!QualitySettings.streamingMipmapsActive)
                {
                    qualityIndexes.Add(i);
                }
            }

            QualitySettings.SetQualityLevel(initialQualityLevel);
            return qualityIndexes;
        }

        internal static void EnableStreamingMipmap(int qualityLevelIndex)
        {
            var initialQualityLevel = QualitySettings.GetQualityLevel();

            QualitySettings.SetQualityLevel(qualityLevelIndex);
            QualitySettings.streamingMipmapsActive = true;

            QualitySettings.SetQualityLevel(initialQualityLevel);
        }
    }
}
