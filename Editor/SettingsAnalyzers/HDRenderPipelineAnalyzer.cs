
using UnityEditor;
using UnityEngine.Rendering.HighDefinition;
#if UNITY_2019_3_OR_NEWER
using UnityEngine.Rendering;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;
#endif

namespace Unity.ProjectAuditor.Editor.SettingsAnalyzers
{
    public class HDRenderPipelineAnalyzer : ISettingsAnalyzer
    {
        static readonly ProblemDescriptor k_Descriptor = new ProblemDescriptor(
            202001,
            "HDRP: Shader mode is set to Both",
            string.Format("{0}|{1}", Area.BuildSize, Area.BuildTime),
            "If shader mode is set to Both, shaders will be built for both Forward and Deferred rendering. This increases build time and size.",
            "Change Shader mode to Forward or Deferred."
        );

        public void Initialize(IAuditor auditor)
        {
            auditor.RegisterDescriptor(k_Descriptor);
        }

        public int GetDescriptorId()
        {
            return k_Descriptor.id;
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
                    if (hdrpAsset.currentPlatformRenderPipelineSettings.supportedLitShaderMode == RenderPipelineSettings.SupportedLitShaderMode.Both)
                        return new ProjectIssue(k_Descriptor, k_Descriptor.description, IssueCategory.ProjectSettings);
                }
            }
#endif

            return null;
        }
    }
}
