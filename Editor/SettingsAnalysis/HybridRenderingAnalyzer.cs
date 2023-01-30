using System;
using System.Collections.Generic;
using UnityEditor;
using Unity.ProjectAuditor.Editor.Core;
using Unity.ProjectAuditor.Editor.Diagnostic;
using Unity.ProjectAuditor.Editor.Modules;
using Unity.ProjectAuditor.Editor.Utils;

namespace Unity.ProjectAuditor.Editor.SettingsAnalysis
{
    class HybridRenderingAnalyzer : ISettingsModuleAnalyzer
    {
        static readonly Descriptor k_Descriptor = new Descriptor(
            "PAS1000",
            "Player Settings: Static batching is enabled",
            Area.CPU,
            "Static batching is enabled and the package com.unity.rendering.hybrid is installed. Static batching is incompatible with the batching techniques used in the Hybrid Renderer and Scriptable Render Pipeline, and will result in poor rendering performance and excessive memory use.",
            "Disable static batching in Player Settings"
        );

        public void Initialize(ProjectAuditorModule module)
        {
            module.RegisterDescriptor(k_Descriptor);
        }

        public IEnumerable<ProjectIssue> Analyze(ProjectAuditorParams projectAuditorParams)
        {
#if PACKAGE_HYBRID_RENDERER

            if (PlayerSettingsUtil.IsStaticBatchingEnabled(projectAuditorParams.platform))
            {
                yield return ProjectIssue.Create(IssueCategory.ProjectSetting, k_Descriptor);
            }
#else
            yield break;
#endif
        }
    }
}
