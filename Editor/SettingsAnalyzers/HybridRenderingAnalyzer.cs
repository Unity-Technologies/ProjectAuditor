#if HYBRID_RENDERER_ANALYZER_SUPPORT

using System;
using System.Collections.Generic;
using UnityEditor;

namespace Unity.ProjectAuditor.Editor.SettingsAnalyzers
{
    class HybridRenderingAnalyzer : ISettingsAnalyzer
    {
        static readonly ProblemDescriptor k_Descriptor = new ProblemDescriptor(
            202000,
            "Player Settings: Static batching is enabled",
            Area.CPU,
            "Static batching is enabled and the package com.unity.rendering.hybrid is installed. Static batching is incompatible with the batching techniques used in the Hybrid Renderer and Scriptable Render Pipeline, and will result in poor rendering performance and excessive memory use.",
            "Disable static batching in Player Settings"
        );

        public void Initialize(ProjectAuditorModule module)
        {
            module.RegisterDescriptor(k_Descriptor);
        }

        public IEnumerable<ProjectIssue> Analyze(BuildTarget platform)
        {
            if (Evaluators.PlayerSettingsIsStaticBatchingEnabled(platform))
            {
                yield return new ProjectIssue(k_Descriptor, IssueCategory.ProjectSetting);
            }
        }
    }
}
#endif
