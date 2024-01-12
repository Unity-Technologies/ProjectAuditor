using System.Collections.Generic;
using System.Linq;
using Unity.ProjectAuditor.Editor.Core;
using Unity.ProjectAuditor.Editor.Diagnostic;
using Unity.ProjectAuditor.Editor.Interfaces;
using Unity.ProjectAuditor.Editor.Modules;
using Unity.ProjectAuditor.Editor.Utils;
using UnityEditor;
using UnityEditor.Build;

namespace Unity.ProjectAuditor.Editor.SettingsAnalysis
{
    class StrippingAnalyzer : ISettingsModuleAnalyzer
    {
        internal const string PAS0009 = nameof(PAS0009);
        internal const string PAS0025 = nameof(PAS0025);
        internal const string PAS0026 = nameof(PAS0026);

        static readonly Descriptor k_EngineCodeStrippingDescriptor = new Descriptor(
            PAS0009,
            "Player: Engine Code Stripping is disabled",
            Areas.BuildSize,
            "The <b>Strip Engine Code</b> is option in Player Settings is disabled. The generated build will be larger than necessary.",
            "Enable <b>Strip Engine Code</b> in <b>Project Settings > Player > Other Settings > Optimization</b>.")
        {
            Platforms = new[] { BuildTarget.Android, BuildTarget.iOS, BuildTarget.WebGL }
        };

        static readonly Descriptor k_AndroidManagedStrippingDescriptor = new Descriptor(
            PAS0025,
            "Player (Android): Managed Code Stripping is set to Disabled or Low",
            Areas.BuildSize,
#if UNITY_2021_2_OR_NEWER
            "The <b>Managed Stripping Level</b> in the Android Player Settings is set to <b>Disabled</b>, <b>Low</b> or <b>Minimal</b>. The generated build will be larger than necessary.",
#else
            "The <b>Managed Stripping Level</b> in the Android Player Settings is set to <b>Disabled</b> or <b>Low</b>. The generated build will be larger than necessary.",
#endif
            "Set <b>Managed Stripping Level</b> in the Android Player Settings to Medium or High.")
        {
            Platforms = new[] { BuildTarget.Android }
        };

        static readonly Descriptor k_iOSManagedStrippingDescriptor = new Descriptor(
            PAS0026,
            "Player (iOS): Managed Code Stripping is set to Disabled, Low or Minimal",
            Areas.BuildSize,
            "The <b>Managed Stripping Level</b> in the iOS Player Settings is set to <b>Disabled</b>, <b>Low</b> or <b>Minimal</b>. The generated build will be larger than necessary.",
            "Set <b>Managed Stripping Level</b> in the iOS Player Settings to Medium or High.")
        {
            Platforms = new[] { BuildTarget.iOS }
        };

        public void Initialize(Module module)
        {
            module.RegisterDescriptor(k_EngineCodeStrippingDescriptor);
            module.RegisterDescriptor(k_AndroidManagedStrippingDescriptor);
            module.RegisterDescriptor(k_iOSManagedStrippingDescriptor);
        }

        public IEnumerable<ReportItem> Analyze(SettingsAnalysisContext context)
        {
            if (k_EngineCodeStrippingDescriptor.IsApplicable(context.Params) && !PlayerSettings.stripEngineCode)
            {
                yield return context.CreateIssue(IssueCategory.ProjectSetting, k_EngineCodeStrippingDescriptor.Id)
                    .WithLocation("Project/Player");
            }

            if (k_AndroidManagedStrippingDescriptor.IsApplicable(context.Params))
            {
                var value = PlayerSettingsUtil.GetManagedStrippingLevel(BuildTargetGroup.Android);
                if (value == ManagedStrippingLevel.Disabled || value == ManagedStrippingLevel.Low
#if UNITY_2021_2_OR_NEWER
                    || value == ManagedStrippingLevel.Minimal
#endif
                )
                    yield return context.CreateIssue(IssueCategory.ProjectSetting, k_AndroidManagedStrippingDescriptor.Id)
                        .WithLocation("Project/Player");
            }

            if (k_iOSManagedStrippingDescriptor.IsApplicable(context.Params))
            {
                var value = PlayerSettingsUtil.GetManagedStrippingLevel(BuildTargetGroup.iOS);
                if (value == ManagedStrippingLevel.Disabled || value == ManagedStrippingLevel.Low)
                    yield return context.CreateIssue(IssueCategory.ProjectSetting, k_iOSManagedStrippingDescriptor.Id)
                        .WithLocation("Project/Player");
            }
        }
    }
}
