using System.Collections.Generic;
using Unity.ProjectAuditor.Editor.Core;
using Unity.ProjectAuditor.Editor.Diagnostic;
using Unity.ProjectAuditor.Editor.Modules;
using UnityEngine;
using UnityEngine.Rendering;
#if PACKAGE_URP
using UnityEngine.Rendering.Universal;
#endif

namespace Unity.ProjectAuditor.Editor.SettingsAnalysis
{
    class UniversalRenderPipelineAnalyzer : ISettingsModuleAnalyzer
    {
        internal const string PAS1009 = nameof(PAS1009);
        internal const string PAS1010 = nameof(PAS1010);
        internal const string PAS1011 = nameof(PAS1011);

        static readonly Descriptor k_URPAssetDescriptor = new Descriptor(
            PAS1009,
            "URP: URP Asset is not specified",
            new[] {Area.GPU, Area.Quality},
            "Project graphics settings do not refer to URP Asset.",
            "Check the settings: Graphics > Scriptable Render Pipeline Settings, Quality > Render Pipeline Asset.")
        {
            messageFormat = "URP: URP Asset is not specified"
        };

        static readonly Descriptor k_HdrSettingDescriptor = new Descriptor(
            PAS1010,
            "URP: HDR is enabled",
            new[] {Area.GPU, Area.Quality},
            "High Dynamic Range (HDR) is enabled in URP Asset for mobile platforms. HDR rendering can be very intensive on low-end mobile GPUs.",
            "Disable HDR in URP Asset.")
        {
            platforms = new[] {"Android", "iOS", "Switch"},
            messageFormat = "URP: HDR is enabled in {0}.asset in {1}",
            fixer = FixHdrSetting
        };

        static readonly Descriptor k_MsaaSampleCountSettingDescriptor = new Descriptor(
            PAS1011,
            "URP: MSAA is set to 4x or 8x",
            new[] {Area.GPU, Area.Quality},
            "Multi-sample anti-aliasing (MSAA) is set to 4x or 8x in URP Asset for mobile platforms. MSAA 4x/8x rendering can be intensive on low-end mobile GPUs.",
            "Decrease MSAA value to 2x in URP Asset.")
        {
            platforms = new[] {"Android", "iOS", "Switch"},
            messageFormat = "URP: MSAA is set to 4x or 8x in {0}.asset in {1}",
            fixer = FixMsaaSampleCountSetting
        };


        public void Initialize(ProjectAuditorModule module)
        {
            module.RegisterDescriptor(k_URPAssetDescriptor);
            module.RegisterDescriptor(k_HdrSettingDescriptor);
            module.RegisterDescriptor(k_MsaaSampleCountSettingDescriptor);
        }

        public IEnumerable<ProjectIssue> Analyze(ProjectAuditorParams projectAuditorParams)
        {
#if UNITY_2019_3_OR_NEWER && PACKAGE_URP
            var renderPipeline = GraphicsSettings.currentRenderPipeline;
            if (renderPipeline == null || renderPipeline is not UniversalRenderPipelineAsset)
            {
                yield return ProjectIssue.Create(IssueCategory.ProjectSetting, k_URPAssetDescriptor)
                    .WithLocation("Project/Graphics");
            }
            else
            {
#if UNITY_ANDROID || UNITY_IOS || UNITY_SWITCH
                foreach (var analyzeSrpAsset in SrpAssetSettingsAnalyzer.AnalyzeSrpAssets(Analyze)) yield return analyzeSrpAsset;
#endif
            }
#else
            yield break;
#endif
        }

        private static void FixHdrSetting(ProjectIssue issue)
        {
#if UNITY_2019_3_OR_NEWER
            SrpAssetSettingsAnalyzer.FixSetting(issue, p => SetHdrSetting(p, false));
#endif
        }

        private static void FixMsaaSampleCountSetting(ProjectIssue issue)
        {
#if UNITY_2019_3_OR_NEWER
            SrpAssetSettingsAnalyzer.FixSetting(issue, p => SetMsaaSampleCountSetting(p, 2));
#endif
        }

#if UNITY_2019_3_OR_NEWER
        private IEnumerable<ProjectIssue> Analyze(RenderPipelineAsset renderPipeline, int qualityLevel)
        {
#if PACKAGE_URP
            bool? supportsHDR = GetHdrSetting(renderPipeline);
            if (supportsHDR != null && supportsHDR.Value)
            {
                yield return SrpAssetSettingsAnalyzer.CreateAssetSettingsIssue(qualityLevel, renderPipeline.name,
                    k_HdrSettingDescriptor);
            }

            int? msaaSampleCount = GetMsaaSampleCountSetting(renderPipeline);
            if (msaaSampleCount != null && msaaSampleCount >= 4)
            {
                yield return SrpAssetSettingsAnalyzer.CreateAssetSettingsIssue(qualityLevel, renderPipeline.name,
                    k_MsaaSampleCountSettingDescriptor);
            }
#else
            yield break;
#endif
        }

        internal static bool? GetHdrSetting(RenderPipelineAsset renderPipeline)
        {
            if (renderPipeline == null) return null;
#if PACKAGE_URP
            if (renderPipeline is UniversalRenderPipelineAsset urpAsset)
            {
                return urpAsset.supportsHDR;
            }
#endif
            return null;
        }

        internal static void SetHdrSetting(RenderPipelineAsset renderPipeline, bool value)
        {
            if (renderPipeline == null) return;
#if PACKAGE_URP
            if (renderPipeline is UniversalRenderPipelineAsset urpAsset)
            {
                urpAsset.supportsHDR = value;
            }
#endif
        }

        internal static int? GetMsaaSampleCountSetting(RenderPipelineAsset renderPipeline)
        {
            if (renderPipeline == null) return null;
#if PACKAGE_URP
            if (renderPipeline is UniversalRenderPipelineAsset urpAsset)
            {
                return urpAsset.msaaSampleCount;
            }
#endif
            return null;
        }

        internal static void SetMsaaSampleCountSetting(RenderPipelineAsset renderPipeline, int value)
        {
            if (renderPipeline == null) return;
#if PACKAGE_URP
            if (renderPipeline is UniversalRenderPipelineAsset urpAsset)
            {
                urpAsset.msaaSampleCount = value;
            }
#endif
        }
#endif

    }
}
