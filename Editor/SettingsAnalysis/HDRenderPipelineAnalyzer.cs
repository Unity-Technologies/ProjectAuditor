using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Unity.ProjectAuditor.Editor.Core;
using Unity.ProjectAuditor.Editor.Diagnostic;
using Unity.ProjectAuditor.Editor.Modules;

namespace Unity.ProjectAuditor.Editor.SettingsAnalysis
{
    class HDRenderPipelineAnalyzer : ISettingsModuleAnalyzer
    {
        static readonly Descriptor k_AssetLitShaderModeBothOrMixed = new Descriptor(
            "PAS1001",
            "HDRP: Render Pipeline Assets use both 'Lit Shader Mode' Forward and Deferred",
            new[] { Area.BuildSize, Area.BuildTime },
            "If HDRP 'Lit Shader Mode' is set to Both (or a mix of Forward and Deferred), shaders will be built for both Forward and Deferred rendering. This increases build time and size.",
            "Change Shader mode to Forward or Deferred."
        );

        static readonly Descriptor k_CameraLitShaderModeBothOrMixed = new Descriptor(
            "PAS1002",
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

#if PACKAGE_HDRP
        public IEnumerable<ProjectIssue> Analyze(ProjectAuditorParams projectAuditorParams)
        {
            if (IsLitShaderModeBothOrMixed())
            {
                var deferredCamera = false;
                var forwardCamera = false;
                var allCameraData = new List<UnityEngine.Rendering.HighDefinition.HDAdditionalCameraData>();
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
                    if (cameraData.renderingPathCustomFrameSettings.litShaderMode == UnityEngine.Rendering.HighDefinition.LitShaderMode.Deferred)
                        deferredCamera = true;
                    else
                        forwardCamera = true;

                    if (deferredCamera && forwardCamera)
                        yield return ProjectIssue.Create(IssueCategory.ProjectSetting, k_CameraLitShaderModeBothOrMixed);
                }
                yield return ProjectIssue.Create(IssueCategory.ProjectSetting, k_AssetLitShaderModeBothOrMixed);
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

        void GetCameraComponents(GameObject go, ref List<UnityEngine.Rendering.HighDefinition.HDAdditionalCameraData> components)
        {
            var comp = go.GetComponent(typeof(UnityEngine.Rendering.HighDefinition.HDAdditionalCameraData));
            if (comp != null)
                components.Add((UnityEngine.Rendering.HighDefinition.HDAdditionalCameraData)comp);
            for (int i = 0; i < go.transform.childCount; i++)
            {
                GetCameraComponents(go.transform.GetChild(i).gameObject, ref components);
            }
        }

#else
        public IEnumerable<ProjectIssue> Analyze(ProjectAuditorParams projectAuditorParams)
        {
            yield break;
        }

#endif
    }
}
