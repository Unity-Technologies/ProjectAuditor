using System.Collections.Generic;
using Unity.ProjectAuditor.Editor.Core;
using Unity.ProjectAuditor.Editor.Diagnostic;
using Unity.ProjectAuditor.Editor.Modules;
using UnityEngine.Rendering;
#if PACKAGE_URP
using UnityEngine.Rendering.Universal;
#elif PACKAGE_HDRP
using System.Reflection;
using UnityEngine.Rendering.HighDefinition;
#endif

namespace Unity.ProjectAuditor.Editor.SettingsAnalysis
{
    class SrpAssetSettingsAnalyzer : ISettingsModuleAnalyzer
    {
        internal const string PAS1008 = nameof(PAS1008);

        static readonly Descriptor k_SRPBatcherSettingDescriptor = new Descriptor(
            PAS1008,
            "SRP Asset: SRP Batcher",
            Area.CPU,
            "SRP batcher is disabled in Render Pipeline Asset.",
            "Enable SRP batcher in Render Pipeline Asset. This will reduce the CPU time Unity requires to prepare and dispatch draw calls for materials that use the same shader variant.")
        {
            messageFormat = "SRP batcher is disabled in {0}.asset in {1}",
            fixer = FixSrpBatcherSetting
        };

        public void Initialize(ProjectAuditorModule module)
        {
            module.RegisterDescriptor(k_SRPBatcherSettingDescriptor);
        }

        public IEnumerable<ProjectIssue> Analyze(ProjectAuditorParams projectAuditorParams)
        {
#if UNITY_2019_3_OR_NEWER
            return RenderPipelineUtils.AnalyzeAssets(Analyze);
#else
            yield break;
#endif
        }

        private static void FixSrpBatcherSetting(ProjectIssue issue)
        {
#if UNITY_2019_3_OR_NEWER
            RenderPipelineUtils.FixAssetSetting(issue, p => SetSrpBatcherSetting(p, true));
#endif
        }

#if UNITY_2019_3_OR_NEWER
        private IEnumerable<ProjectIssue> Analyze(RenderPipelineAsset renderPipeline, int qualityLevel)
        {
            bool? srpBatcherSetting = GetSrpBatcherSetting(renderPipeline);
            if (srpBatcherSetting != null && !srpBatcherSetting.Value)
            {
                yield return CreateSrpBatcherIssue(qualityLevel, renderPipeline.name);
            }
        }

        private static ProjectIssue CreateSrpBatcherIssue(int qualityLevel, string name)
        {
            return RenderPipelineUtils.CreateAssetSettingIssue(qualityLevel, name, k_SRPBatcherSettingDescriptor);
        }

        internal static bool? GetSrpBatcherSetting(RenderPipelineAsset renderPipeline)
        {
            if (renderPipeline == null) return null;
#if PACKAGE_URP
            if (renderPipeline is UniversalRenderPipelineAsset urpAsset)
            {
                return urpAsset.useSRPBatcher;
            }
#elif PACKAGE_HDRP
            FieldInfo enableSrpBatcherField = GetSrpBatcherField(renderPipeline,
                out HDRenderPipelineAsset hdrpAsset);
            if (enableSrpBatcherField != null)
            {
                return (bool)enableSrpBatcherField.GetValue(hdrpAsset);
            }
#endif
            return null;
        }

        internal static void SetSrpBatcherSetting(RenderPipelineAsset renderPipeline, bool value)
        {
            if (renderPipeline == null) return;
#if PACKAGE_URP
            if (renderPipeline is UniversalRenderPipelineAsset urpAsset)
            {
                urpAsset.useSRPBatcher = value;
            }
#elif PACKAGE_HDRP
            FieldInfo enableSrpBatcherField = GetSrpBatcherField(renderPipeline,
                out HDRenderPipelineAsset hdrpAsset);
            if (enableSrpBatcherField != null)
            {
                enableSrpBatcherField.SetValue(hdrpAsset, value);
            }
#endif
        }

#if PACKAGE_HDRP
        private static FieldInfo GetSrpBatcherField(RenderPipelineAsset renderPipeline,
            out HDRenderPipelineAsset hdrpAsset)
        {
            hdrpAsset = null;
            if (renderPipeline is HDRenderPipelineAsset asset)
            {
                hdrpAsset = asset;
                return hdrpAsset.GetType()
                    .GetField("enableSRPBatcher", BindingFlags.NonPublic | BindingFlags.Instance);
            }

            return null;
        }

#endif
#endif
    }
}
