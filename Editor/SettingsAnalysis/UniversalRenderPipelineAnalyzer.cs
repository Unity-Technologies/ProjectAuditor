using System.Collections.Generic;
using Unity.ProjectAuditor.Editor.Core;
using Unity.ProjectAuditor.Editor.Diagnostic;
using Unity.ProjectAuditor.Editor.Interfaces;
using Unity.ProjectAuditor.Editor.Modules;
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
        internal const string PAS1012 = nameof(PAS1012);

        static readonly Descriptor k_URPAssetDescriptor = new Descriptor(
            PAS1009,
            "URP: URP Asset is not specified",
            new[] {Area.GPU, Area.Quality},
            "Graphics Settings do not refer to a URP Asset.",
            "Check the settings: Graphics > Scriptable Render Pipeline Settings > Render Pipeline Asset.")
        {
            messageFormat = "URP: URP Asset is not specified"
        };

        static readonly Descriptor k_HdrSettingDescriptor = new Descriptor(
            PAS1010,
            "URP: HDR is enabled",
            new[] {Area.GPU, Area.Quality},
            "<b>HDR</b> (High Dynamic Range) is enabled in a URP Asset for mobile platforms. HDR rendering can be very intensive on low-end mobile GPUs.",
            "Disable <b>HDR</b> in the URP Asset.")
        {
            platforms = new[] {"Android", "iOS", "Switch"},
            messageFormat = "URP: HDR is enabled in {0}.asset in {1}",
            fixer = FixHdrSetting
        };

        static readonly Descriptor k_MsaaSampleCountSettingDescriptor = new Descriptor(
            PAS1011,
            "URP: MSAA is set to 4x or 8x",
            new[] {Area.GPU, Area.Quality},
            "<b>Anti Aliasing (MSAA)</b> is set to <b>4x</b> or <b>8x</b> in a URP Asset for mobile platforms. MSAA 4x/8x rendering can be intensive on low-end mobile GPUs.",
            "Decrease <b>Anti Aliasing (MSAA)</b> value to <b>2x</b> in the URP Asset.")
        {
            platforms = new[] {"Android", "iOS", "Switch"},
            messageFormat = "URP: MSAA is set to 4x or 8x in {0}.asset in {1}",
            fixer = FixMsaaSampleCountSetting
        };

        static readonly Descriptor k_CameraStopNanDescriptor = new Descriptor(
            PAS1012,
            "URP: Stop NaN property is enabled",
            Area.GPU,
            "The <b>Stop NaNs</b> property is enabled on a Camera component. This stops certain effects from breaking, but is a resource-intensive process on the GPU. Only enable this feature if you experience NaN issues that you cannot fix.",
            "Disable <b>Stop NaNs</b> on as Camera components as you can."
        );

        public void Initialize(ProjectAuditorModule module)
        {
            module.RegisterDescriptor(k_URPAssetDescriptor);
            module.RegisterDescriptor(k_HdrSettingDescriptor);
            module.RegisterDescriptor(k_MsaaSampleCountSettingDescriptor);
            module.RegisterDescriptor(k_CameraStopNanDescriptor);
        }

        public IEnumerable<ProjectIssue> Analyze(ProjectAuditorParams projectAuditorParams)
        {
#if UNITY_2019_3_OR_NEWER && PACKAGE_URP
            var renderPipeline = GraphicsSettings.currentRenderPipeline;
            if (renderPipeline == null || !(renderPipeline is UniversalRenderPipelineAsset))
            {
                yield return ProjectIssue.Create(IssueCategory.ProjectSetting, k_URPAssetDescriptor.id)
                    .WithLocation("Project/Graphics");
            }
            else
            {
#if UNITY_ANDROID || UNITY_IOS || UNITY_SWITCH
                foreach (var analyzeSrpAsset in RenderPipelineUtils.AnalyzeAssets(Analyze)) yield return analyzeSrpAsset;

                var allCameraData = RenderPipelineUtils
                    .GetAllComponents<UniversalAdditionalCameraData>();
                foreach (var cameraData in allCameraData)
                {
                    if (cameraData.stopNaN)
                        yield return ProjectIssue.Create(IssueCategory.ProjectSetting,
                            k_CameraStopNanDescriptor.id);
                }
#endif
            }
#else
            yield break;
#endif
        }

        private static void FixHdrSetting(ProjectIssue issue)
        {
#if UNITY_2019_3_OR_NEWER && PACKAGE_URP
            RenderPipelineUtils.FixAssetSetting(issue, p => SetHdrSetting(p, false));
#endif
        }

        private static void FixMsaaSampleCountSetting(ProjectIssue issue)
        {
#if UNITY_2019_3_OR_NEWER && PACKAGE_URP
            RenderPipelineUtils.FixAssetSetting(issue, p => SetMsaaSampleCountSetting(p, 2));
#endif
        }

#if UNITY_2019_3_OR_NEWER
        private IEnumerable<ProjectIssue> Analyze(RenderPipelineAsset renderPipeline, int qualityLevel)
        {
#if PACKAGE_URP
            if (GetHdrSetting(renderPipeline))
            {
                yield return RenderPipelineUtils.CreateAssetSettingIssue(qualityLevel, renderPipeline.name,
                    k_HdrSettingDescriptor.id);
            }

            if (GetMsaaSampleCountSetting(renderPipeline) >= 4)
            {
                yield return RenderPipelineUtils.CreateAssetSettingIssue(qualityLevel, renderPipeline.name,
                    k_MsaaSampleCountSettingDescriptor.id);
            }
#else
            yield break;
#endif
        }

#if PACKAGE_URP
        internal static bool GetHdrSetting(RenderPipelineAsset renderPipeline)
        {
            return renderPipeline is UniversalRenderPipelineAsset urpAsset && urpAsset.supportsHDR;
        }

        internal static void SetHdrSetting(RenderPipelineAsset renderPipeline, bool value)
        {
            if (renderPipeline is UniversalRenderPipelineAsset urpAsset)
            {
                urpAsset.supportsHDR = value;
            }
        }

        internal static int GetMsaaSampleCountSetting(RenderPipelineAsset renderPipeline)
        {
            return renderPipeline is UniversalRenderPipelineAsset urpAsset ? urpAsset.msaaSampleCount : -1;
        }

        internal static void SetMsaaSampleCountSetting(RenderPipelineAsset renderPipeline, int value)
        {
            if (renderPipeline is UniversalRenderPipelineAsset urpAsset)
            {
                urpAsset.msaaSampleCount = value;
            }
        }

#endif
#endif
    }
}
