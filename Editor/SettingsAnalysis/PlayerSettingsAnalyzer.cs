using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Unity.ProjectAuditor.Editor.Core;
using Unity.ProjectAuditor.Editor.Diagnostic;
using Unity.ProjectAuditor.Editor.Modules;
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

        static readonly Descriptor k_AccelerometerDescriptor = new Descriptor(
            PAS0002,
            "Player (iOS): Accelerometer",
            new[] { Area.CPU },
            "The Accelerometer is enabled in iOS Player Settings.",
            "Consider setting <b>Accelerometer Frequency</b> to Disabled if your application doesn't make use of the device's accelerometer. Disabling this option will save a tiny amount of CPU processing time.")
        {
            platforms = new[] { BuildTarget.iOS.ToString() }
        };

        static readonly Descriptor k_SplashScreenDescriptor = new Descriptor(
            PAS0029,
            "Player: Splash Screen",
            new[] { Area.LoadTime },
            "<b>Splash Screen</b> is enabled and will increase the time it takes to load into the first scene.",
            "Disable the Splash Screen option in <b>Project Settings ➔ Player ➔ Splash Image ➔ Show Splash Screen</b>.");

        static readonly Descriptor k_SpeakerModeDescriptor = new Descriptor(
            PAS0033,
            "Audio: Speaker Mode",
            new[] { Area.BuildSize },
            "<b>UnityEngine.AudioSettings.speakerMode</b> is not set to <b>UnityEngine.AudioSpeakerMode.Mono</b>. The generated build will be larger than necessary.",
            "To reduce runtime memory consumption of AudioClips change <b>Project Settings ➔ Audio ➔ Default Speaker Mode</b> to <b>Mono</b>. This will half memory usage of stereo AudioClips. It is also recommended considering enabling the <b>Force To Mono</b> AudioClip import setting to reduce import times and build size.")
        {
            platforms = new[] { "Android", "iOS"},
            fixer = (issue => {
                FixSpeakerMode();
            })
        };

        static readonly Descriptor k_IL2CPPCompilerConfigurationMasterDescriptor = new Descriptor(
            PAS1004,
            "Player: IL2CPP Compiler Configuration",
            new[] { Area.BuildTime },
            "<b>C++ Compiler Configuration</b> is set to <b>Master</b>. Keep this mode only for shipping builds.",
            "To have optimal build time, change <b>Project Settings ➔ Configuration ➔ C++ Compiler Configuration</b> to <b>Release</b>.")
        {
            fixer = (issue => {
                SetIL2CPPConfigurationToRelease();
            }),

            messageFormat = "Player : C++ Compiler Configuration is set to 'Master'. The build time will be longer."
        };

        private static readonly Descriptor k_IL2CPPCompilerConfigurationDebugDescriptor = new Descriptor(
            PAS1005,
            "Player: IL2CPP Compiler Configuration",
            new[] { Area.CPU },
            "<b>C++ Compiler Configuration</b> is set to <b>Debug</b>. The performances will be suboptimal. Keep this mode only for debugging only.",
            "To have optimal performances, change <b>Project Settings ➔ Configuration ➔ C++ Compiler Configuration</b> to <b>Release</b>.")
        {
            fixer = (issue =>
            {
                SetIL2CPPConfigurationToRelease();
            }),

            messageFormat = "Player : C++ Compiler Configuration is set to 'Debug'. The performances will be suboptimal."
        };

        public void Initialize(ProjectAuditorModule module)
        {
            module.RegisterDescriptor(k_AccelerometerDescriptor);
            module.RegisterDescriptor(k_SplashScreenDescriptor);
            module.RegisterDescriptor(k_SpeakerModeDescriptor);
            module.RegisterDescriptor(k_IL2CPPCompilerConfigurationMasterDescriptor);
            module.RegisterDescriptor(k_IL2CPPCompilerConfigurationDebugDescriptor);
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
            if (CheckIL2CPPCompilerConfiguration(Il2CppCompilerConfiguration.Master))
            {
                yield return ProjectIssue.Create(IssueCategory.ProjectSetting, k_IL2CPPCompilerConfigurationMasterDescriptor)
                    .WithLocation("Project/Player");
            }
            if (CheckIL2CPPCompilerConfiguration(Il2CppCompilerConfiguration.Debug))
            {
                yield return ProjectIssue.Create(IssueCategory.ProjectSetting, k_IL2CPPCompilerConfigurationDebugDescriptor)
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

        internal static bool CheckIL2CPPCompilerConfiguration(Il2CppCompilerConfiguration compilerConfiguration)
        {
            var buildTargetGroup = EditorUserBuildSettings.selectedBuildTargetGroup;
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
