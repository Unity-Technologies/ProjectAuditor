using System.Collections.Generic;
using Unity.ProjectAuditor.Editor.Core;
using Unity.ProjectAuditor.Editor.Diagnostic;
using Unity.ProjectAuditor.Editor.Interfaces;
using UnityEditor;

#if UNITY_2020_1_OR_NEWER
namespace Unity.ProjectAuditor.Editor.SettingsAnalysis
{
    class EditorSettingsAnalyzer : ISettingsModuleAnalyzer
    {
        internal const string PAS0035 = nameof(PAS0035);
        internal const string PAS0036 = nameof(PAS0036);

        private static readonly Descriptor k_EnterPlayModeOptionsDescriptor = new Descriptor(
            PAS0035,
            "Editor: Enter Play Mode Options is not enabled",
            new[] { Area.IterationTime },
            "The <b>Enter Play Mode Options</b> option in Editor Settings is not enabled. Without enabling this option, you cannot disable Domain Reload, meaning that entering Play Mode will take longer every time.",
            "In Editor Settings, enable the <b>Enter Play Mode Settings > Enter Play Mode Options</b> option, then disable the <b>Reload Domain</b> option. Be sure to view the <b>Code/Domain Reload</b> view in this tool for additional things you may need to fix as a result of disabling domain reload."
        )
        {
            maximumVersion = "2023.4",
            fixer = (issue) =>
            {
                EditorSettings.enterPlayModeOptionsEnabled = true;
            }
        };

        private static readonly Descriptor k_DomainReloadDescriptor = new Descriptor(
            PAS0036,
            "Editor: Reload Domain is enabled",
            new[] { Area.IterationTime },
            "The <b>Reload Domain</b> option In Editor Settings is enabled. If Reload Domain is enabled, the entire script state will be reloaded when entering and exiting Play Mode, and after every code change. This can considerably slow down iteration time.",
            "In Editor Settings, enable the <b>Enter Play Mode Settings > Enter Play Mode Options</b> option, then disable the <b>Reload Domain</b> checkbox. Be sure to view the <b>Code/Domain Reload</b> view in this tool for additional things you may need to fix as a result of disabling domain reload."
        )
        {
            maximumVersion = "2023.4",
            fixer = (issue) =>
            {
                EditorSettings.enterPlayModeOptions |= EnterPlayModeOptions.DisableDomainReload;
            }
        };

        public void Initialize(Module module)
        {
            module.RegisterDescriptor(k_EnterPlayModeOptionsDescriptor);
            module.RegisterDescriptor(k_DomainReloadDescriptor);
        }

        public IEnumerable<ProjectIssue> Analyze(SettingsAnalysisContext context)
        {
            if (k_EnterPlayModeOptionsDescriptor.IsVersionCompatible() &&
                !EditorSettings.enterPlayModeOptionsEnabled)
            {
                yield return context.Create(IssueCategory.ProjectSetting, k_EnterPlayModeOptionsDescriptor.id)
                    .WithLocation("Project/Editor");
            }
            else
            {
                if (k_DomainReloadDescriptor.IsVersionCompatible() &&
                    (EditorSettings.enterPlayModeOptions & EnterPlayModeOptions.DisableDomainReload) != EnterPlayModeOptions.DisableDomainReload)
                {
                    yield return context.Create(IssueCategory.ProjectSetting, k_DomainReloadDescriptor.id)
                        .WithLocation("Project/Editor");
                }
            }
        }
    }
}
#endif
