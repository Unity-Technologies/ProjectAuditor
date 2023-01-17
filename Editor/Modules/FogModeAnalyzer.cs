using System.Collections.Generic;
using System.Linq;
using Unity.ProjectAuditor.Editor.Core;
using Unity.ProjectAuditor.Editor.Diagnostic;
using Unity.ProjectAuditor.Editor.Modules;
using UnityEngine;

namespace Unity.ProjectAuditor.Editor.Modules
{
    class FogModeAnalyzer : ISettingsModuleAnalyzer
    {
        private static readonly Descriptor k_fogModeDescriptor = new Descriptor(
            "PAS1003",
            "Graphics: Fog Mode",
            new[] {Area.BuildSize},
            "Enabling Fog will result in additional shader variants, thus increasing build size.",
            "To reduce the build size, turn off Fog at <b>Window ➔ Lighting ➔ Environment ➔ Other Settings ➔ Fog</b> to <b>Mono</b>.")
        {
            fixer = (issue =>
            {
                DisableFog();
            })
        };

        public void Initialize(ProjectAuditorModule module)
        {
            module.RegisterDescriptor(k_fogModeDescriptor);
        }

        public IEnumerable<ProjectIssue> Analyze(ProjectAuditorParams projectAuditorParams)
        {
            if (IsFogEnable())
            {
                yield return ProjectIssue.Create(IssueCategory.ProjectSetting, k_fogModeDescriptor)
                    .WithLocation("Project/Graphics");
            }
        }

        internal static bool IsFogEnable()
        {
            return RenderSettings.fog;
        }

        internal static void DisableFog()
        {
            RenderSettings.fog = false;
        }
    }
}
