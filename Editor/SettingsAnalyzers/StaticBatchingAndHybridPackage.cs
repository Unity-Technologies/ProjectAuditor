using System;
using System.Reflection;
using Unity.ProjectAuditor.Editor.Auditors;
using UnityEditor;
#if UNITY_2019_3_OR_NEWER
using PackageInfo = UnityEditor.PackageManager.PackageInfo;
#endif

namespace Unity.ProjectAuditor.Editor.SettingsAnalyzers
{
    internal class StaticBatchingAndHybridPackage : ISettingsAnalyzer
    {
        private static readonly ProblemDescriptor s_Descriptor = new ProblemDescriptor(
            202000,
            "Player Settings: Static batching is enabled",
            Area.CPU,
            "Static batching is enabled and the package com.unity.rendering.hybrid is installed. Static batching is incompatible with the batching techniques used in the Hybrid Renderer and Scriptable Render Pipeline, and will result in poor rendering performance and excessive memory use.",
            "Disable static batching in Player Settings"
        );

        public void Initialize(IAuditor auditor)
        {
            auditor.RegisterDescriptor(s_Descriptor);
        }

        public int GetDescriptorId()
        {
            return s_Descriptor.id;
        }

        public ProjectIssue Analyze()
        {
#if UNITY_2019_3_OR_NEWER
            if (PackageInfo.FindForAssetPath("Packages/com.unity.rendering.hybrid") != null && IsStaticBatchingEnabled(EditorUserBuildSettings.activeBuildTarget))
            {
                return new ProjectIssue(s_Descriptor, s_Descriptor.description, IssueCategory.ProjectSettings);
            }
#endif
            return null;
        }

        private static bool IsStaticBatchingEnabled(BuildTarget platform)
        {
            var method = typeof(PlayerSettings).GetMethod("GetBatchingForPlatform",
                BindingFlags.Static | BindingFlags.Default | BindingFlags.NonPublic);
            if (method == null) throw new NotSupportedException("Getting batching per platform is not supported");

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
