using System;
using System.Collections.Generic;
using UnityEditor;
using Unity.ProjectAuditor.Editor.Core;
using Unity.ProjectAuditor.Editor.Diagnostic;
using Unity.ProjectAuditor.Editor.Interfaces;
using Unity.ProjectAuditor.Editor.Modules;
using Unity.ProjectAuditor.Editor.Utils;

namespace Unity.ProjectAuditor.Editor.SettingsAnalysis
{
    class HybridRenderingAnalyzer : ISettingsModuleAnalyzer
    {
        internal const string PAS1000 = nameof(PAS1000);

        static readonly Descriptor k_Descriptor = new Descriptor(
            PAS1000,
            "Player Settings: Static batching is enabled",
            Area.CPU,
            "<b>Static Batching</b> is enabled in Player Settings and the package com.unity.rendering.hybrid is installed. Static batching is incompatible with the batching techniques used in the Hybrid Renderer and Scriptable Render Pipeline, and will result in poor rendering performance and excessive memory use.",
            "Disable static batching in Player Settings."
        );

        public void Initialize(Module module)
        {
            module.RegisterDescriptor(k_Descriptor);
        }

        public IEnumerable<ProjectIssue> Analyze(SettingsAnalysisContext context)
        {
#if PACKAGE_HYBRID_RENDERER

            if (PlayerSettingsUtil.IsStaticBatchingEnabled(context.Params.Platform))
            {
                yield return context.Create(IssueCategory.ProjectSetting, k_Descriptor.Id);
            }
#else
            yield break;
#endif
        }
    }
}
