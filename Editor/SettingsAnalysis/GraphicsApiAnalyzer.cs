using System.Collections.Generic;
using System.Linq;
using Unity.ProjectAuditor.Editor.Core;
using Unity.ProjectAuditor.Editor.Diagnostic;
using Unity.ProjectAuditor.Editor.Interfaces;
using UnityEditor;
using UnityEngine.Rendering;

namespace Unity.ProjectAuditor.Editor.SettingsAnalysis
{
    class GraphicsApiAnalyzer : ISettingsModuleAnalyzer
    {
        const string documentationUrl = "https://docs.unity3d.com/Manual/GraphicsAPIs.html";

        internal const string PAS0005 = nameof(PAS0005);
        internal const string PAS0006 = nameof(PAS0006);
        internal const string PAS0031 = nameof(PAS0031);

        static readonly Descriptor k_OpenGLESAndMetalDescriptor = new Descriptor(
            PAS0005,
            "Player (iOS): Metal & OpenGLES APIs are both enabled",
            Areas.BuildSize,
            "In the iOS Player Settings, both Metal and OpenGLES graphics APIs are enabled.",
            "To reduce build size, remove OpenGLES graphics API if the minimum spec target device supports Metal.")
        {
            DocumentationUrl = documentationUrl,
            Platforms = new[] { BuildTarget.iOS },
            MaximumVersion = "2022.3"
        };

        static readonly Descriptor k_MetalDescriptor = new Descriptor(
            PAS0006,
            "Player (iOS): Metal API is not enabled",
            Areas.CPU,
            "In the iOS Player Settings, Metal is not enabled.",
            "Enable Metal graphics API for better CPU Performance.")
        {
            DocumentationUrl = documentationUrl,
            Platforms = new[] { BuildTarget.iOS }
        };

        static readonly Descriptor k_VulkanDescriptor = new Descriptor(
            PAS0031,
            "Player (Android): Vulkan API is not enabled",
            Areas.CPU | Areas.GPU,
            "In the Android Player Settings, Vulkan graphics API is not enabled.",
            "Enable Vulkan graphics API for better CPU Performance.")
        {
            DocumentationUrl = documentationUrl,
            Platforms = new[] { BuildTarget.Android }
        };

        public void Initialize(Module module)
        {
            module.RegisterDescriptor(k_OpenGLESAndMetalDescriptor);
            module.RegisterDescriptor(k_MetalDescriptor);
            module.RegisterDescriptor(k_VulkanDescriptor);
        }

        public IEnumerable<ProjectIssue> Analyze(SettingsAnalysisContext context)
        {
            if (k_OpenGLESAndMetalDescriptor.IsApplicable(context.Params) && IsUsingOpenGLESAndMetal())
                yield return context.Create(IssueCategory.ProjectSetting, k_OpenGLESAndMetalDescriptor.Id)
                    .WithLocation("Project/Player");

            if (k_MetalDescriptor.IsApplicable(context.Params) && IsNotUsingMetal())
                yield return context.Create(IssueCategory.ProjectSetting, k_MetalDescriptor.Id)
                    .WithLocation("Project/Player");

            if (k_VulkanDescriptor.IsApplicable(context.Params) && IsNotUsingVulkan())
                yield return context.Create(IssueCategory.ProjectSetting, k_VulkanDescriptor.Id)
                    .WithLocation("Project/Player");
        }

        static bool IsNotUsingMetal()
        {
            var graphicsAPIs = PlayerSettings.GetGraphicsAPIs(BuildTarget.iOS);

            var hasMetal = graphicsAPIs.Contains(GraphicsDeviceType.Metal);

            return !hasMetal;
        }

        static bool IsUsingOpenGLESAndMetal()
        {
#if UNITY_2023_1_OR_NEWER
            return false;
#else
            var graphicsAPIs = PlayerSettings.GetGraphicsAPIs(BuildTarget.iOS);

            var hasOpenGLES = graphicsAPIs.Contains(GraphicsDeviceType.OpenGLES2) ||
                graphicsAPIs.Contains(GraphicsDeviceType.OpenGLES3);

            return graphicsAPIs.Contains(GraphicsDeviceType.Metal) && hasOpenGLES;
#endif
        }

        static bool IsNotUsingVulkan()
        {
            return !PlayerSettings.GetGraphicsAPIs(BuildTarget.Android).Contains(GraphicsDeviceType.Vulkan);
        }
    }
}
