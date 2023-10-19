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
    class BuiltinRenderPipelineAnalyzer : ISettingsModuleAnalyzer
    {
        static readonly GraphicsTier[] k_GraphicsTiers = { GraphicsTier.Tier1, GraphicsTier.Tier2, GraphicsTier.Tier3};

        internal const string PAS0022 = nameof(PAS0022);
        internal const string PAS0023 = nameof(PAS0023);
        internal const string PAS0024 = nameof(PAS0024);

        static readonly Descriptor k_ShaderQualityDescriptor = new Descriptor(
            PAS0022,
            "Graphics: Shader Quality uses a mixture of different values",
            new[] { Area.BuildSize },
            "The current build target Graphics Tier Settings use a mixture of different values (Low/Medium/High) for the <b>Standard Shader Quality</b> setting. This will result in a larger number of shader variants being compiled, which will increase build times and your application's download/install size.",
            "Unless you support devices with a very wide range of capabilities for a particular platform, consider editing the platform in Graphics Settings to use the same shader quality setting across all Graphics Tiers.");

        static readonly Descriptor k_ForwardRenderingDescriptor = new Descriptor(
            PAS0023,
            "Graphics: Rendering Path is set to Forward Rendering",
            new[] { Area.GPU },
            "The current build target uses forward rendering, as set in the <b>Rendering Path</b> settings in <b>Graphics Settings ➔ Tier Settings</b>. This can impact GPU performance in projects with nontrivial numbers of dynamic lights.",
            "This rendering path is suitable for games with simple rendering and lighting requirements - for instance, 2D games, or games which mainly use baked lighting. If the project makes use of a more than a few dynamic lights, consider experimenting with changing <b>Rendering Path</b> to Deferred to see whether doing so improves GPU rendering times.");

        static readonly Descriptor k_DeferredRenderingDescriptor = new Descriptor(
            PAS0024,
            "Graphics: Rendering Path is set to Deferred Rendering",
            new[] { Area.GPU },
            "The current build target uses deferred rendering, as set in the <b>Rendering Path</b> settings in <b>Graphics Settings ➔ Tier Settings</b>. This can impact GPU performance in projects with simple rendering requirements.",
            "This rendering path is suitable for games with more complex rendering requirements - for instance, games that make uses of dynamic lighting or certain types of fullscreen post-processing effects. If the project doesn't make use of such rendering techniques, consider experimenting with changing <b>Rendering Path</b> to Forward to see whether doing so improves GPU rendering times.");

        public void Initialize(ProjectAuditorModule module)
        {
            module.RegisterDescriptor(k_ShaderQualityDescriptor);
            module.RegisterDescriptor(k_ForwardRenderingDescriptor);
            module.RegisterDescriptor(k_DeferredRenderingDescriptor);
        }

        public IEnumerable<ProjectIssue> Analyze(ProjectAuditorParams projectAuditorParams)
        {
            // Only check for Built-In Rendering Pipeline
            if (!IsUsingBuiltinRenderPipeline())
            {
                if (IsMixedStandardShaderQuality(projectAuditorParams.Platform))
                {
                    yield return ProjectIssue.Create(IssueCategory.ProjectSetting, k_ShaderQualityDescriptor.id)
                        .WithLocation("Project/Graphics");
                }
                if (IsUsingForwardRendering(projectAuditorParams.Platform))
                {
                    yield return ProjectIssue.Create(IssueCategory.ProjectSetting, k_ForwardRenderingDescriptor.id)
                        .WithLocation("Project/Graphics");
                }
                if (IsUsingDeferredRendering(projectAuditorParams.Platform))
                {
                    yield return ProjectIssue.Create(IssueCategory.ProjectSetting, k_DeferredRenderingDescriptor.id)
                        .WithLocation("Project/Graphics");
                }
            }
        }

        static bool IsUsingBuiltinRenderPipeline()
        {
#if UNITY_2019_3_OR_NEWER
            return GraphicsSettings.defaultRenderPipeline == null;
#else
            return true;
#endif
        }

        internal static bool IsMixedStandardShaderQuality(BuildTarget platform)
        {
            var buildGroup = BuildPipeline.GetBuildTargetGroup(platform);
            var standardShaderQualities = k_GraphicsTiers.Select(tier => EditorGraphicsSettings.GetTierSettings(buildGroup, tier).standardShaderQuality);

            return standardShaderQualities.Distinct().Count() > 1;
        }

        internal static bool IsUsingForwardRendering(BuildTarget platform)
        {
            var buildGroup = BuildPipeline.GetBuildTargetGroup(platform);
            var renderingPaths = k_GraphicsTiers.Select(tier => EditorGraphicsSettings.GetTierSettings(buildGroup, tier).renderingPath);

            return renderingPaths.Any(path => path == RenderingPath.Forward);
        }

        internal static bool IsUsingDeferredRendering(BuildTarget platform)
        {
            var buildGroup = BuildPipeline.GetBuildTargetGroup(platform);
            var renderingPaths = k_GraphicsTiers.Select(tier => EditorGraphicsSettings.GetTierSettings(buildGroup, tier).renderingPath);

            return renderingPaths.Any(path => path == RenderingPath.DeferredShading);
        }
    }
}
