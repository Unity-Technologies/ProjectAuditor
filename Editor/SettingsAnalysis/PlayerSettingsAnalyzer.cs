using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Unity.ProjectAuditor.Editor.Core;
using Unity.ProjectAuditor.Editor.Diagnostic;
using Unity.ProjectAuditor.Editor.Interfaces;
using Unity.ProjectAuditor.Editor.Modules;
using Unity.ProjectAuditor.Editor.Utils;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;

namespace Unity.ProjectAuditor.Editor.SettingsAnalysis
{
    class PlayerSettingsAnalyzer : ISettingsModuleAnalyzer
    {
        internal const string PAS0002 = nameof(PAS0002);
        internal const string PAS0029 = nameof(PAS0029);
        internal const string PAS0033 = nameof(PAS0033);
        internal const string PAS1004 = nameof(PAS1004);
        internal const string PAS1005 = nameof(PAS1005);
        internal const string PAS1006 = nameof(PAS1006);

        static readonly Descriptor k_AccelerometerDescriptor = new Descriptor(
            PAS0002,
            "Player (iOS): Accelerometer is enabled",
            new[] { Area.CPU },
            "<b>Accelerometer Frequency</b> in iOS Player Settings is not set to Disabled. Polling the device's accelerometer incurs a small amount of CPU processing time.",
            "Set <b>Accelerometer Frequency</b> to <b>Disabled</b> if your application doesn't make use of the device's accelerometer.")
        {
            platforms = new[] { BuildTarget.iOS.ToString() }
        };

        static readonly Descriptor k_SplashScreenDescriptor = new Descriptor(
            PAS0029,
            "Player: Splash Screen is enabled",
            new[] { Area.LoadTime },
            "<b>Show Splash Screen</b> is enabled in the Player Settings. Displaying a splash screen will increase the time it takes to load into the first scene.",
            "Disable the Splash Screen option in <b>Player Settings ➔ Splash Image ➔ Show Splash Screen</b>.");

        static readonly Descriptor k_SpeakerModeDescriptor = new Descriptor(
            PAS0033,
            "Audio: Speaker Mode is not set to Mono",
            new[] { Area.BuildSize, Area.Memory },
            "<b>Default Speaker Mode</b> in Audio Settings is not set to <b>Mono</b>. This may result in a build which is larger than necessary and which occupies more audio memory at runtime. Many mobile devices have limited or nonexistent stereo speaker options.",
            "Change <b>Audio Settings ➔ Default Speaker Mode</b> to <b>Mono</b>. You should also consider enabling the <b>Force To Mono</b> AudioClip import setting to reduce import times and build size.")
        {
            platforms = new[] { "Android", "iOS"},
            fixer = (issue => {
                FixSpeakerMode();
            })
        };

        static readonly Descriptor k_IL2CPPCompilerConfigurationMasterDescriptor = new Descriptor(
            PAS1004,
            "Player: IL2CPP Compiler Configuration is set to Master",
            new[] { Area.BuildTime },
            "<b>C++ Compiler Configuration</b> in Player Settings is set to <b>Master</b>. This mode is intended for shipping builds and will significantly increase build times.",
            "Change <b>Player Settings ➔ Other Settings ➔ Configuration ➔ C++ Compiler Configuration</b> to <b>Release</b>.")
        {
            fixer = (issue => {
                SetIL2CPPConfigurationToRelease();
            }),

            messageFormat = "Player: C++ Compiler Configuration is set to 'Master'"
        };

        static readonly Descriptor k_IL2CPPCompilerConfigurationDebugDescriptor = new Descriptor(
            PAS1005,
            "Player: IL2CPP Compiler Configuration is set to Debug",
            new[] { Area.CPU },
            "<b>C++ Compiler Configuration</b> is set to <b>Debug</b>. This mode is intended for debugging and might have an impact on runtime CPU performance.",
            "Change <b>Player Settings ➔ Other Settings ➔ Configuration ➔ C++ Compiler Configuration</b> to <b>Release</b>.")
        {
            fixer = (issue =>
            {
                SetIL2CPPConfigurationToRelease();
            }),

            messageFormat = "Player: C++ Compiler Configuration is set to 'Debug'"
        };

        static readonly Descriptor k_LightmapStreamingEnabledDescriptor = new Descriptor(
            PAS1006,
            "Player: Lightmap Streaming is disabled",
            new[] { Area.GPU, Area.CPU },
            "<b>Lightmap Streaming</b> in Player Settings is not enabled. As a result, all lightmap detail levels are loaded into GPU memory, potentially resulting in excessive lightmap texture memory usage.",
            "Enable <b>Lightmap Streaming</b> in <b>Player Settings ➔ Other Settings ➔ Rendering</b>.")
        {
            fixer = (issue =>
            {
                var buildGroupTarget = (BuildTargetGroup)issue.GetCustomPropertyInt32(0);
                PlayerSettingsUtil.SetLightmapStreaming(buildGroupTarget, true);
            }),
        };

        public void Initialize(ProjectAuditorModule module)
        {
            module.RegisterDescriptor(k_AccelerometerDescriptor);
            module.RegisterDescriptor(k_SplashScreenDescriptor);
            module.RegisterDescriptor(k_SpeakerModeDescriptor);
            module.RegisterDescriptor(k_IL2CPPCompilerConfigurationMasterDescriptor);
            module.RegisterDescriptor(k_IL2CPPCompilerConfigurationDebugDescriptor);
            module.RegisterDescriptor(k_LightmapStreamingEnabledDescriptor);
        }

        public IEnumerable<ProjectIssue> Analyze(ProjectAuditorParams projectAuditorParams)
        {
            if (k_AccelerometerDescriptor.platforms.Contains(projectAuditorParams.platform.ToString()) && IsAccelerometerEnabled())
            {
                yield return ProjectIssue.Create(IssueCategory.ProjectSetting, k_AccelerometerDescriptor)
                    .WithLocation("Project/Player");
            }
            if (IsSplashScreenEnabledAndCanBeDisabled())
            {
                yield return ProjectIssue.Create(IssueCategory.ProjectSetting, k_SplashScreenDescriptor)
                    .WithLocation("Project/Player");
            }
            if (!IsSpeakerModeMono())
            {
                yield return ProjectIssue.Create(IssueCategory.ProjectSetting, k_SpeakerModeDescriptor)
                    .WithLocation("Project/Player");
            }
            if (CheckIL2CPPCompilerConfiguration(Il2CppCompilerConfiguration.Master, projectAuditorParams))
            {
                yield return ProjectIssue.Create(IssueCategory.ProjectSetting, k_IL2CPPCompilerConfigurationMasterDescriptor)
                    .WithLocation("Project/Player");
            }
            if (CheckIL2CPPCompilerConfiguration(Il2CppCompilerConfiguration.Debug, projectAuditorParams))
            {
                yield return ProjectIssue.Create(IssueCategory.ProjectSetting, k_IL2CPPCompilerConfigurationDebugDescriptor)
                    .WithLocation("Project/Player");
            }

            var buildTargetGroup = BuildPipeline.GetBuildTargetGroup(projectAuditorParams.platform);
            if (!PlayerSettingsUtil.IsLightmapStreamingEnabled(buildTargetGroup))
            {
                yield return ProjectIssue.Create(IssueCategory.ProjectSetting, k_LightmapStreamingEnabledDescriptor).
                    WithCustomProperties(new object[] {buildTargetGroup})
                    .WithLocation("Project/Player");
            }
        }

        internal static bool IsAccelerometerEnabled()
        {
            return PlayerSettings.accelerometerFrequency != 0;
        }

        internal static bool IsSplashScreenEnabledAndCanBeDisabled()
        {
            if (!PlayerSettings.SplashScreen.show)
                return false;
            var type = Type.GetType("UnityEditor.PlayerSettingsSplashScreenEditor,UnityEditor.dll");
            if (type == null)
                return false;

            var licenseAllowsDisablingProperty = type.GetProperty("licenseAllowsDisabling", BindingFlags.Static | BindingFlags.NonPublic);
            if (licenseAllowsDisablingProperty == null)
                return false;
            return (bool)licenseAllowsDisablingProperty.GetValue(null, null);
        }

        internal static bool IsSpeakerModeMono()
        {
            return AudioSettings.GetConfiguration().speakerMode == AudioSpeakerMode.Mono;
        }

        internal static void FixSpeakerMode()
        {
            AudioConfiguration audioConfiguration = new AudioConfiguration
            {
                speakerMode = AudioSpeakerMode.Mono
            };

            AudioSettings.Reset(audioConfiguration);
        }

        internal static bool CheckIL2CPPCompilerConfiguration(Il2CppCompilerConfiguration compilerConfiguration, ProjectAuditorParams projectAuditorParams)
        {
            var buildTargetGroup = BuildPipeline.GetBuildTargetGroup(projectAuditorParams.platform);
            if (PlayerSettings.GetScriptingBackend(buildTargetGroup) !=
                ScriptingImplementation.IL2CPP)
            {
                return false;
            }

            return PlayerSettings.GetIl2CppCompilerConfiguration(buildTargetGroup) ==
                compilerConfiguration;
        }

        internal static void SetIL2CPPConfigurationToRelease()
        {
            PlayerSettings.SetIl2CppCompilerConfiguration(EditorUserBuildSettings.selectedBuildTargetGroup, Il2CppCompilerConfiguration.Release);
        }
    }
}
