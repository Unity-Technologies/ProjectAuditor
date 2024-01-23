using System;
using System.Collections.Generic;
using Unity.ProjectAuditor.Editor.Core;
#if PACKAGE_HDRP
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
#endif

namespace Unity.ProjectAuditor.Editor.SettingsAnalysis
{
    class HdrpAnalyzer : SettingsModuleAnalyzer
    {
        internal const string PAS1001 = nameof(PAS1001);
        internal const string PAS1002 = nameof(PAS1002);

        static readonly Descriptor k_AssetLitShaderModeBothOrMixed = new Descriptor(
            PAS1001,
            "HDRP: Render Pipeline Assets use both Lit Shader Modes",
            Areas.BuildSize | Areas.BuildTime,
            "The <b>Lit Shader Mode</b> option in the HDRP Asset is set to <b>Both</b>. As a result, shaders will be built for both Forward and Deferred rendering. This increases build time and size.",
            "Change <b>Lit Shader Mode</b> to either <b>Forward</b> or <b>Deferred</b>."
        );

        static readonly Descriptor k_CameraLitShaderModeBothOrMixed = new Descriptor(
            PAS1002,
            "HDRP: Cameras mix usage of Lit Shader Modes",
            Areas.BuildSize | Areas.BuildTime,
            "Project contains Multiple HD Cameras, some of which have <b>Lit Shader Mode</b> set to <b>Forward</b>, and some to <b>Deferred</b>. As a result, shaders will be built for both Forward and Deferred rendering. This increases build time and size.",
            "Change the <b>Lit Shader Mode</b> in all HDRP Assets and all Cameras to either <b>Forward</b> or <b>Deferred</b>."
        );

        public override void Initialize(Action<Descriptor> registerDescriptor)
        {
            registerDescriptor(k_AssetLitShaderModeBothOrMixed);
            registerDescriptor(k_CameraLitShaderModeBothOrMixed);
        }

#if PACKAGE_HDRP
        public override IEnumerable<ReportItem> Analyze(SettingsAnalysisContext context)
        {
            if (IsLitShaderModeBothOrMixed())
            {
                var deferredCamera = false;
                var forwardCamera = false;
                var allCameraData = RenderPipelineUtils
                    .GetAllComponents<UnityEngine.Rendering.HighDefinition.HDAdditionalCameraData>();
                foreach (var cameraData in allCameraData)
                {
                    if (cameraData.renderingPathCustomFrameSettings.litShaderMode ==
                        UnityEngine.Rendering.HighDefinition.LitShaderMode.Deferred)
                        deferredCamera = true;
                    else
                        forwardCamera = true;

                    if (deferredCamera && forwardCamera)
                        yield return context.CreateIssue(IssueCategory.ProjectSetting,
                            k_CameraLitShaderModeBothOrMixed.Id);
                }

                yield return context.CreateIssue(IssueCategory.ProjectSetting, k_AssetLitShaderModeBothOrMixed.Id);
            }
        }

        bool IsLitShaderModeBothOrMixed()
        {
            // first gather all hdrp assets
            var hdrpAssets = new HashSet<UnityEngine.Rendering.HighDefinition.HDRenderPipelineAsset>();
            if (GraphicsSettings.defaultRenderPipeline is UnityEngine.Rendering.HighDefinition.HDRenderPipelineAsset defaultRenderPipeline)
            {
                hdrpAssets.Add(defaultRenderPipeline);
            }

            for (int i = 0, c = QualitySettings.names.Length; i < c; ++i)
            {
                if (QualitySettings.GetRenderPipelineAssetAt(i) is UnityEngine.Rendering.HighDefinition.HDRenderPipelineAsset hdrpAsset)
                {
                    hdrpAssets.Add(hdrpAsset);
                }
            }

            // then check if any uses SupportedLitShaderMode.Both or a mix of Forward and Deferred
            return hdrpAssets.Any(asset => asset.currentPlatformRenderPipelineSettings.supportedLitShaderMode ==
                UnityEngine.Rendering.HighDefinition.RenderPipelineSettings.SupportedLitShaderMode.Both) ||
                hdrpAssets.Where(asset => asset.currentPlatformRenderPipelineSettings.supportedLitShaderMode !=
                UnityEngine.Rendering.HighDefinition.RenderPipelineSettings.SupportedLitShaderMode.Both).Select(asset =>
                        asset.currentPlatformRenderPipelineSettings.supportedLitShaderMode)
                    .Distinct().Count() > 1;
        }

#else
        public override IEnumerable<ReportItem> Analyze(SettingsAnalysisContext context)
        {
            yield break;
        }

#endif
    }
}
