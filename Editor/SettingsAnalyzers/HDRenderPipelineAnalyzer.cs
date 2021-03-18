
using UnityEditor;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.SceneManagement;
#if UNITY_2019_3_OR_NEWER
using System;
using UnityEngine;
using UnityEngine.Rendering;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;
#endif

namespace Unity.ProjectAuditor.Editor.SettingsAnalyzers
{
    public class HDRenderPipelineAnalyzer : ISettingsAnalyzer
    {
        static readonly ProblemDescriptor k_LitShaderModeBoth = new ProblemDescriptor(
            202001,
            "HDRP: Shader mode is set to Both",
            string.Format("{0}|{1}", Area.BuildSize, Area.BuildTime),
            "If shader mode is set to Both, shaders will be built for both Forward and Deferred rendering. This increases build time and size.",
            "Change Shader mode to Forward or Deferred."
        );

        static readonly ProblemDescriptor k_LitShaderModeBothAndMixedCameras = new ProblemDescriptor(
            202002,
            "HDRP: Shader mode is set to Both",
            string.Format("{0}|{1}", Area.BuildSize, Area.BuildTime),
            "If shader mode is set to Both, shaders will be built for both Forward and Deferred rendering. This increases build time and size.",
            "Change Shader mode to Forward or Deferred."
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

        public ProjectIssue Analyze()
        {
#if UNITY_2019_3_OR_NEWER
            if (PackageInfo.FindForAssetPath("Packages/com.unity.render-pipelines.high-definition") != null)
            {
                var renderPipelineAsset = GraphicsSettings.currentRenderPipeline;
                var hdrpAsset = renderPipelineAsset as HDRenderPipelineAsset;
                if (hdrpAsset != null)
                {
                    if (hdrpAsset.currentPlatformRenderPipelineSettings.supportedLitShaderMode ==
                        RenderPipelineSettings.SupportedLitShaderMode.Both)
                    {
                        var deferredCamera = false;
                        var forwardCamera = false;
                        for (int n = 0; n < SceneManager.sceneCount; ++n)
                        {
                            var scene = SceneManager.GetSceneAt(n);
                            var roots = scene.GetRootGameObjects();
                            foreach (var go in roots)
                            {
                                var renderingPath = go.GetComponent<Camera>().renderingPath;
                                switch (renderingPath)
                                {
                                    case RenderingPath.DeferredLighting:
                                        deferredCamera = true;
                                        break;
                                    case RenderingPath.Forward:
                                        forwardCamera = true;
                                        break;
                                    default:
                                        throw new Exception("Unexpected RenderingPath " + renderingPath);
                                }
                            }
                        }
                        if (deferredCamera && forwardCamera)
                            return new ProjectIssue(k_LitShaderModeBothAndMixedCameras, k_LitShaderModeBothAndMixedCameras.description, IssueCategory.ProjectSettings);
                        return new ProjectIssue(k_LitShaderModeBoth, k_LitShaderModeBoth.description, IssueCategory.ProjectSettings);
                    }
                }
            }
#endif

            return null;
        }
    }
}
