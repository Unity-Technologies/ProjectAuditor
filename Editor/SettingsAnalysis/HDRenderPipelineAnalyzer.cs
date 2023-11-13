using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Unity.ProjectAuditor.Editor.Core;
using Unity.ProjectAuditor.Editor.Diagnostic;
using Unity.ProjectAuditor.Editor.Interfaces;
using Unity.ProjectAuditor.Editor.Modules;

namespace Unity.ProjectAuditor.Editor.SettingsAnalysis
{
    class HDRenderPipelineAnalyzer : ISettingsModuleAnalyzer
    {
        internal const string PAS1001 = nameof(PAS1001);
        internal const string PAS1002 = nameof(PAS1002);

        static readonly Descriptor k_AssetLitShaderModeBothOrMixed = new Descriptor(
            PAS1001,
            "HDRP: Render Pipeline Assets use both Lit Shader Modes",
            new[] { Area.BuildSize, Area.BuildTime },
            "The <b>Lit Shader Mode</b> option in the HDRP Asset is set to <b>Both</b>. As a result, shaders will be built for both Forward and Deferred rendering. This increases build time and size.",
            "Change <b>Lit Shader Mode</b> to either <b>Forward</b> or <b>Deferred</b>."
        );

        static readonly Descriptor k_CameraLitShaderModeBothOrMixed = new Descriptor(
            PAS1002,
            "HDRP: Cameras mix usage of Lit Shader Modes",
            new[] { Area.BuildSize, Area.BuildTime },
            "Project contains Multiple HD Cameras, some of which have <b>Lit Shader Mode</b> set to <b>Forward</b>, and some to <b>Deferred</b>. As a result, shaders will be built for both Forward and Deferred rendering. This increases build time and size.",
            "Change the <b>Lit Shader Mode</b> in all HDRP Assets and all Cameras to either <b>Forward</b> or <b>Deferred</b>."
        );

        public void Initialize(Module module)
        {
            module.RegisterDescriptor(k_AssetLitShaderModeBothOrMixed);
            module.RegisterDescriptor(k_CameraLitShaderModeBothOrMixed);
        }

#if PACKAGE_HDRP
        public IEnumerable<ProjectIssue> Analyze(SettingsAnalysisContext context)
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
                        yield return context.Create(IssueCategory.ProjectSetting,
                            k_CameraLitShaderModeBothOrMixed.id);
                }

                yield return context.Create(IssueCategory.ProjectSetting, k_AssetLitShaderModeBothOrMixed.id);
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
        public IEnumerable<ProjectIssue> Analyze(SettingsAnalysisContext context)
        {
            yield break;
        }

#endif
    }
}
