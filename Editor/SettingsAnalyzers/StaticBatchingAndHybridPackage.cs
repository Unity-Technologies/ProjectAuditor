using System;
using System.Reflection;
using Unity.ProjectAuditor.Editor.Auditors;
using UnityEditor;
#if UNITY_2019_3_OR_NEWER
using PackageInfo = UnityEditor.PackageManager.PackageInfo;
#endif

namespace Unity.ProjectAuditor.Editor.SettingsAnalyzers
{
    [Attribute]
    public class StaticBatchingAndHybridPackage : ISettingsAnalyzer
    {
        private static readonly ProblemDescriptor descriptor = new ProblemDescriptor
        {
            id = 202000,
            description = "Player Settings: Static batching is enabled",
            area = "CPU",
            problem =
                "Static batching is enabled and the package com.unity.rendering.hybrid is installed. Static batching is incompatible with the batching techniques used in the Hybrid Renderer and Scriptable Render Pipeline, and will result in poor rendering performance and excessive memory use.",
            solution = "Disable static batching in Player Settings"
        };

        public StaticBatchingAndHybridPackage(SettingsAuditor auditor)
        {
            auditor.RegisterDescriptor(descriptor);
        }

        public int GetDescriptorId()
        {
            return descriptor.id;
        }

        public ProjectIssue Analyze()
        {
#if UNITY_2019_3_OR_NEWER
            if (PackageInfo.FindForAssetPath("Packages/com.unity.rendering.hybrid") != null && IsStaticBatchingEnabled(EditorUserBuildSettings.activeBuildTarget))
            {
                return new ProjectIssue(descriptor, descriptor.description, IssueCategory.ProjectSettings);
            }
#endif
            return null;
        }

        private static bool IsStaticBatchingEnabled(BuildTarget platform)
        {
            var method = typeof(PlayerSettings).GetMethod("GetBatchingForPlatform",
                BindingFlags.Static | BindingFlags.Default | BindingFlags.NonPublic);
            if (method == null) throw new NotSupportedException("Getting batching per platform is not supported");

            var staticBatching = 0;
            var dynamicBatching = 0;
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
