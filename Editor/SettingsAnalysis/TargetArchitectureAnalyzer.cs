using System.Collections.Generic;
using Unity.ProjectAuditor.Editor.Core;
using Unity.ProjectAuditor.Editor.Diagnostic;
using Unity.ProjectAuditor.Editor.Modules;
using UnityEditor;

namespace Unity.ProjectAuditor.Editor.SettingsAnalysis
{
    class TargetArchitectureAnalyzer : ISettingsModuleAnalyzer
    {
        internal const string PAS0003 = nameof(PAS0003);
        internal const string PAS0004 = nameof(PAS0004);

        static readonly Descriptor k_DescriptorIOS = new Descriptor(
            PAS0003,
            "Player (iOS): Building multiple architectures",
            Area.BuildSize,
            "In the iOS Player Settings, <b>Architecture</b> is set to Universal. This means that the application will be compiled for both 32-bit ARMv7 iOS devices (i.e. up to the iPhone 5 or 5c) and 64-bit ARM64 devices (iPhone 5s onwards), resulting in increased build times and binary size.",
            "If your application isn't intended to support 32-bit iOS devices, change <b>Architecture</b> to ARM64.")
        {
            platforms = new string[] { BuildTarget.iOS.ToString() }
        };

        static readonly Descriptor k_DescriptorAndroid = new Descriptor(
            PAS0004,
            "Player (Android): Building multiple architectures",
            Area.BuildSize,
            "In the Android Player Settings, in the <b>Target Architecture</b> section, both the <b>ARMv7</b> and <b>ARM64</b> options are selected. This means that the application will be compiled for both 32-bit ARMv7 Android devices and 64-bit ARM64 devices, resulting in increased build times and binary size.",
            "If your application isn't intended to support 32-bit Android devices, disable the <b>ARMv7</b> option.")
        {
            platforms = new string[] { BuildTarget.Android.ToString() }
        };

        public void Initialize(ProjectAuditorModule module)
        {
            module.RegisterDescriptor(k_DescriptorIOS);
            module.RegisterDescriptor(k_DescriptorAndroid);
        }

        public IEnumerable<ProjectIssue> Analyze(ProjectAuditorParams projectAuditorParams)
        {
            // PlayerSettings.GetArchitecture returns an integer value associated with the architecture of a BuildTargetPlatformGroup. 0 - None, 1 - ARM64, 2 - Universal.
            if (projectAuditorParams.platform == BuildTarget.iOS && PlayerSettings.GetArchitecture(BuildTargetGroup.iOS) == 2)
                yield return ProjectIssue.Create(IssueCategory.ProjectSetting, k_DescriptorIOS)
                    .WithLocation("Project/Player");

            if (projectAuditorParams.platform == BuildTarget.Android && (PlayerSettings.Android.targetArchitectures & AndroidArchitecture.ARMv7) != 0 &&
                (PlayerSettings.Android.targetArchitectures & AndroidArchitecture.ARM64) != 0)
                yield return ProjectIssue.Create(IssueCategory.ProjectSetting, k_DescriptorAndroid)
                    .WithLocation("Project/Player");
        }
    }
}
