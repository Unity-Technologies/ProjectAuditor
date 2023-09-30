using System.Collections.Generic;
using System.Linq;
using Unity.ProjectAuditor.Editor.Core;
using Unity.ProjectAuditor.Editor.Diagnostic;
using Unity.ProjectAuditor.Editor.Interfaces;
using Unity.ProjectAuditor.Editor.Modules;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;

namespace Unity.ProjectAuditor.Editor.SettingsAnalysis
{
    class EditorSettingsAnalyzer : ISettingsModuleAnalyzer
    {
        internal const string PAS0035 = nameof(PAS0035);
        internal const string PAS0036 = nameof(PAS0036);

        private static readonly Descriptor k_EnterPlayModeOptionsDescriptor = new Descriptor(
            PAS0035,
            "Editor: Enter Play Mode Options is unticked",
            new[] { Area.IterationTime },
            "In Editor Settings, under <b>Enter Play Mode Settings</b>, the <b>Enter Play Mode Options</b> checkbox is unticked. Without ticking this checkbox, you cannot disable Domain Reload, meaning that entering Play Mode will take longer every time.",
            "In Editor Settings, tick the <b>Enter Play Mode Settings > Enter Play Mode Options</b> checkbox, then untick the <b>Reload Domain</b> checkbox. Be sure to view the <b>Code/Domain Reload</b> view in this tool for additional things you may need to fix as a result of disabling domain reload."
        )
        {
            fixer = (issue) =>
            {
                EditorSettings.enterPlayModeOptionsEnabled = true;
            }
        };

        private static readonly Descriptor k_DomainReloadDescriptor = new Descriptor(
            PAS0036,
            "Editor: Reload Domain is ticked",
            new[] { Area.IterationTime },
            "In Editor Settings, under <b>Enter Play Mode Settings</b>, the <b>Reload Domain</b> checkbox is ticked. If Domain Reload is enabled, the entire script state will be reloaded when entering and exiting Play Mode, and after every code change. This can considerable slow down iteration time.",
            "In Editor Settings, tick the <b>Enter Play Mode Settings > Enter Play Mode Options</b> checkbox, then untick the <b>Reload Domain</b> checkbox. Be sure to view the <b>Code/Domain Reload</b> view in this tool for additional things you may need to fix as a result of disabling domain reload."
        )
        {
            fixer = (issue) =>
            {
                EditorSettings.enterPlayModeOptions |= EnterPlayModeOptions.DisableDomainReload;
            }
        };

        public void Initialize(ProjectAuditorModule module)
        {
            module.RegisterDescriptor(k_EnterPlayModeOptionsDescriptor);
            module.RegisterDescriptor(k_DomainReloadDescriptor);
        }

        public IEnumerable<ProjectIssue> Analyze(ProjectAuditorParams projectAuditorParams)
        {
            if (!EditorSettings.enterPlayModeOptionsEnabled)
            {
                yield return ProjectIssue.Create(IssueCategory.ProjectSetting, k_EnterPlayModeOptionsDescriptor)
                    .WithLocation("Project/Editor");
            }
            else
            {
                if ((EditorSettings.enterPlayModeOptions & EnterPlayModeOptions.DisableDomainReload) != EnterPlayModeOptions.DisableDomainReload)
                {
                    yield return ProjectIssue.Create(IssueCategory.ProjectSetting, k_DomainReloadDescriptor)
                        .WithLocation("Project/Editor");
                }
            }
        }
    }
}
