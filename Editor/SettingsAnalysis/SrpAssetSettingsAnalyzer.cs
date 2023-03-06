using System.Collections.Generic;
using Unity.ProjectAuditor.Editor.Core;
using Unity.ProjectAuditor.Editor.Diagnostic;
using Unity.ProjectAuditor.Editor.Modules;
using UnityEngine;
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
            messageFormat = "SRP batcher is disabled in '{0}' in '{1}'",
            fixer = FixSrpBatcherSetting
        };

        public void Initialize(ProjectAuditorModule module)
        {
            module.RegisterDescriptor(k_SRPBatcherSettingDescriptor);
        }

        public IEnumerable<ProjectIssue> Analyze(ProjectAuditorParams projectAuditorParams)
        {
#if UNITY_2019_3_OR_NEWER
            IEnumerable<ProjectIssue> issues = Analyze(GraphicsSettings.defaultRenderPipeline, -1);
            foreach (ProjectIssue issue in issues)
            {
                yield return issue;
            }

            var initialQualityLevel = QualitySettings.GetQualityLevel();
            for (var i = 0; i < QualitySettings.names.Length; ++i)
            {
                QualitySettings.SetQualityLevel(i);

                issues = Analyze(QualitySettings.renderPipeline, i);
                foreach (ProjectIssue issue in issues)
                {
                    yield return issue;
                }
            }

            QualitySettings.SetQualityLevel(initialQualityLevel);
#else
            yield break;
#endif
        }

        private static void FixSrpBatcherSetting(ProjectIssue issue)
        {
#if UNITY_2019_3_OR_NEWER
            int qualityLevel = issue.GetCustomPropertyInt32(0);
            if (qualityLevel == -1)
            {
                SetSrpBatcherSetting(GraphicsSettings.defaultRenderPipeline);
                return;
            }

            var initialQualityLevel = QualitySettings.GetQualityLevel();
            QualitySettings.SetQualityLevel(qualityLevel);
            SetSrpBatcherSetting(QualitySettings.renderPipeline);
            QualitySettings.SetQualityLevel(initialQualityLevel);
#endif
        }

#if UNITY_2019_3_OR_NEWER
        private IEnumerable<ProjectIssue> Analyze(RenderPipelineAsset renderPipeline, int qualityLevel)
        {
            if (renderPipeline == null) yield break;
#if PACKAGE_URP
            if (renderPipeline is UniversalRenderPipelineAsset urpAsset &&
                !urpAsset.useSRPBatcher)
            {
                yield return CreateSrpBatcherIssue(qualityLevel, urpAsset.name);
            }
#elif PACKAGE_HDRP
            FieldInfo enableSrpBatcherField = GetSrpBatcherField(renderPipeline, out HDRenderPipelineAsset hdrpAsset);
            if (enableSrpBatcherField != null && !(bool)enableSrpBatcherField.GetValue(hdrpAsset))
            {

                yield return CreateSrpBatcherIssue(qualityLevel, hdrpAsset.name);
            }
#endif
        }

        private static ProjectIssue CreateSrpBatcherIssue(int qualityLevel, string name)
        {
            string assetLocation = qualityLevel == -1
                ? "Default Rendering Pipeline Asset"
                : $"Rendering Pipeline Asset in Quality Level: {QualitySettings.names[qualityLevel]}";
            return ProjectIssue.Create(IssueCategory.ProjectSetting, k_SRPBatcherSettingDescriptor,
                    name, assetLocation)
                .WithCustomProperties(new object[] { qualityLevel })
                .WithLocation(qualityLevel == -1 ? "Project/Graphics" : "Project/Quality");
        }

        internal static void SetSrpBatcherSetting(RenderPipelineAsset renderPipeline, bool value = true)
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
