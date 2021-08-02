#if HYBRID_RENDERER_ANALYZER_SUPPORT

using System;
using System.Collections.Generic;
using System.Reflection;
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

        public IEnumerable<ProjectIssue> Analyze()
        {
            if (IsStaticBatchingEnabled(EditorUserBuildSettings.activeBuildTarget))
            {
                yield return new ProjectIssue(k_Descriptor, k_Descriptor.description, IssueCategory.ProjectSetting);
            }
        }

        static bool IsStaticBatchingEnabled(BuildTarget platform)
        {
            var method = typeof(PlayerSettings).GetMethod("GetBatchingForPlatform",
                BindingFlags.Static | BindingFlags.Default | BindingFlags.NonPublic);
            if (method == null)
                throw new NotSupportedException("Getting batching per platform is not supported");

            const int staticBatching = 0;
            const int dynamicBatching = 0;
            var args = new object[]
            {
                platform,
                staticBatching,
                dynamicBatching
            };

            method.Invoke(null, args);
            return (int)args[1] > 0;
        }
    }
}
#endif
