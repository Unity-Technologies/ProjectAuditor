#if HDRP_ANALYZER_SUPPORT

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace Unity.ProjectAuditor.Editor.SettingsAnalyzers
{
    class HDRenderPipelineAnalyzer : ISettingsAnalyzer
    {
        static readonly ProblemDescriptor k_AssetLitShaderModeBothOrMixed = new ProblemDescriptor(
            202001,
            "HDRP: Render Pipeline Assets use both 'Lit Shader Mode' Forward and Deferred",
            new[] { Area.BuildSize, Area.BuildTime },
            "If HDRP 'Lit Shader Mode' is set to Both (or a mix of Forward and Deferred), shaders will be built for both Forward and Deferred rendering. This increases build time and size.",
            "Change Shader mode to Forward or Deferred."
        );

        static readonly ProblemDescriptor k_CameraLitShaderModeBothOrMixed = new ProblemDescriptor(
            202002,
            "HDRP: Cameras mix usage of 'Lit Shader Mode' Forward and Deferred",
            new[] { Area.BuildSize, Area.BuildTime },
            "If Cameras use both 'Lit Shader Mode' Forward and Deferred, shaders will be built for both Forward and Deferred rendering. This increases build time and size.",
            "Change HDRP asset and all Cameras 'Lit Shader Mode' to either Forward or Deferred."
        );

        public void Initialize(ProjectAuditorModule module)
        {
            module.RegisterDescriptor(k_AssetLitShaderModeBothOrMixed);
            module.RegisterDescriptor(k_CameraLitShaderModeBothOrMixed);
        }

        public int GetDescriptorId()
        {
            return k_AssetLitShaderModeBothOrMixed.id;
        }

        public IEnumerable<ProjectIssue> Analyze()
        {
            if (IsLitShaderModeBothOrMixed())
            {
                var deferredCamera = false;
                var forwardCamera = false;
                var allCameraData = new List<HDAdditionalCameraData>();
                for (int n = 0; n < SceneManager.sceneCount; ++n)
                {
                    var scene = SceneManager.GetSceneAt(n);
                    var roots = scene.GetRootGameObjects();
                    foreach (var go in roots)
                    {
                        GetCameraComponents(go, ref allCameraData);
                    }
                }
                foreach (var cameraData in allCameraData)
                {
                    if (cameraData.renderingPathCustomFrameSettings.litShaderMode == LitShaderMode.Deferred)
                        deferredCamera = true;
                    else
                        forwardCamera = true;

                    if (deferredCamera && forwardCamera)
                        yield return new ProjectIssue(k_CameraLitShaderModeBothOrMixed, k_CameraLitShaderModeBothOrMixed.description, IssueCategory.ProjectSetting);
                }
                yield return new ProjectIssue(k_AssetLitShaderModeBothOrMixed, k_AssetLitShaderModeBothOrMixed.description, IssueCategory.ProjectSetting);
            }
        }

        bool IsLitShaderModeBothOrMixed()
        {
            // first gather all hdrp assets
            var hdrpAssets = new HashSet<HDRenderPipelineAsset>();
            if (GraphicsSettings.defaultRenderPipeline is HDRenderPipelineAsset defaultRenderPipeline)
            {
                hdrpAssets.Add(defaultRenderPipeline);
            }

            for (int i = 0, c = QualitySettings.names.Length; i < c; ++i)
            {
                if (QualitySettings.GetRenderPipelineAssetAt(i) is HDRenderPipelineAsset hdrpAsset)
                {
                    hdrpAssets.Add(hdrpAsset);
                }
            }

            // then check if any uses SupportedLitShaderMode.Both or a mix of Forward and Deferred
            return hdrpAssets.Any(asset => asset.currentPlatformRenderPipelineSettings.supportedLitShaderMode ==
                RenderPipelineSettings.SupportedLitShaderMode.Both) ||
                hdrpAssets.Where(asset => asset.currentPlatformRenderPipelineSettings.supportedLitShaderMode !=
                RenderPipelineSettings.SupportedLitShaderMode.Both).Select(asset =>
                        asset.currentPlatformRenderPipelineSettings.supportedLitShaderMode)
                    .Distinct().Count() > 1;
        }

        void GetCameraComponents(GameObject go, ref List<HDAdditionalCameraData> components)
        {
            var comp = go.GetComponent(typeof(HDAdditionalCameraData));
            if (comp != null)
                components.Add((HDAdditionalCameraData)comp);
            for (int i = 0; i < go.transform.childCount; i++)
            {
                GetCameraComponents(go.transform.GetChild(i).gameObject, ref components);
            }
        }
    }
}
#endif
