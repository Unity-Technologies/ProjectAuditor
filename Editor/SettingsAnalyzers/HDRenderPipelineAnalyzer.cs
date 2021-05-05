#if HDRP_ANALYZER_SUPPORT

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace Unity.ProjectAuditor.Editor.SettingsAnalyzers
{
    public class HDRenderPipelineAnalyzer : ISettingsAnalyzer
    {
        static readonly ProblemDescriptor k_LitShaderModeBoth = new ProblemDescriptor(
            202001,
            "HDRP: Shader mode is set to Both",
            new[] { Area.BuildSize, Area.BuildTime },
            "If HDRP 'Lit Shader Mode' is set to Both, shaders will be built for both Forward and Deferred rendering. This increases build time and size.",
            "Change Shader mode to Forward or Deferred."
        );

        static readonly ProblemDescriptor k_LitShaderModeBothAndMixedCameras = new ProblemDescriptor(
            202002,
            "HDRP: Mixed usage of 'Lit Shader Mode' Forward and Deferred",
            new[] { Area.BuildSize, Area.BuildTime },
            "If Cameras use both 'Lit Shader Mode' Forward and Deferred, shaders will be built for both Forward and Deferred rendering. This increases build time and size.",
            "Change HDRP asset and all Cameras 'Lit Shader Mode' to either Forward or Deferred."
        );

        public void Initialize(IAuditor auditor)
        {
            auditor.RegisterDescriptor(k_LitShaderModeBoth);
            auditor.RegisterDescriptor(k_LitShaderModeBothAndMixedCameras);
        }

        public int GetDescriptorId()
        {
            return k_LitShaderModeBoth.id;
        }

        public IEnumerable<ProjectIssue> Analyze()
        {
            if (IsLitShaderModeBoth())
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
                        yield return new ProjectIssue(k_LitShaderModeBothAndMixedCameras, k_LitShaderModeBothAndMixedCameras.description, IssueCategory.ProjectSettings);
                }
                yield return new ProjectIssue(k_LitShaderModeBoth, k_LitShaderModeBoth.description, IssueCategory.ProjectSettings);
            }
        }

        bool IsLitShaderModeBoth()
        {
            var renderPipelineAsset = GraphicsSettings.currentRenderPipeline;
            if (renderPipelineAsset is HDRenderPipelineAsset currentHdrpAsset)
            {
                if (currentHdrpAsset.currentPlatformRenderPipelineSettings.supportedLitShaderMode ==
                    RenderPipelineSettings.SupportedLitShaderMode.Both)
                {
                    return true;
                }
            }

            for (int i = 0, c = QualitySettings.names.Length; i < c; ++i)
            {
                if (QualitySettings.GetRenderPipelineAssetAt(i) is HDRenderPipelineAsset hdrpAsset)
                {
                    if (hdrpAsset.currentPlatformRenderPipelineSettings.supportedLitShaderMode ==
                        RenderPipelineSettings.SupportedLitShaderMode.Both)
                    {
                        return true;
                    }
                }
            }

            return false;
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
