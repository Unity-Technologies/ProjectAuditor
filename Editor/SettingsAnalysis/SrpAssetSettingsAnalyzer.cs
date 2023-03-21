using System;
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
            return AnalyzeSrpAssets(Analyze);
#else
            yield break;
#endif
        }

        private static void FixSrpBatcherSetting(ProjectIssue issue)
        {
#if UNITY_2019_3_OR_NEWER
            FixSetting(issue, p => SetSrpBatcherSetting(p, true));
#endif
        }

#if UNITY_2019_3_OR_NEWER
        public static IEnumerable<ProjectIssue> AnalyzeSrpAssets(Func<RenderPipelineAsset, int, IEnumerable<ProjectIssue>> analyze)
        {

            IEnumerable<ProjectIssue> issues = analyze(GraphicsSettings.defaultRenderPipeline, -1);
            foreach (ProjectIssue issue in issues)
            {
                yield return issue;
            }

            var initialQualityLevel = QualitySettings.GetQualityLevel();
            for (var i = 0; i < QualitySettings.names.Length; ++i)
            {
                QualitySettings.SetQualityLevel(i);

                issues = analyze(QualitySettings.renderPipeline, i);
                foreach (ProjectIssue issue in issues)
                {
                    yield return issue;
                }
            }

            QualitySettings.SetQualityLevel(initialQualityLevel);
        }

        private IEnumerable<ProjectIssue> Analyze(RenderPipelineAsset renderPipeline, int qualityLevel)
        {
            bool? srpBatcherSetting = GetSrpBatcherSetting(renderPipeline);
            if (srpBatcherSetting != null && !srpBatcherSetting.Value)
            {
                yield return CreateSrpBatcherIssue(qualityLevel, renderPipeline.name);
            }
        }

        public static ProjectIssue CreateAssetSettingsIssue(int qualityLevel, string name, Descriptor descriptor)
        {
            string assetLocation = qualityLevel == -1
                ? "Default Rendering Pipeline Asset"
                : $"Rendering Pipeline Asset on Quality Level: '{QualitySettings.names[qualityLevel]}'";
            return ProjectIssue.Create(IssueCategory.ProjectSetting, descriptor,
                name, assetLocation)
                .WithCustomProperties(new object[] { qualityLevel })
                .WithLocation(qualityLevel == -1 ? "Project/Graphics" : "Project/Quality");
        }

        private static ProjectIssue CreateSrpBatcherIssue(int qualityLevel, string name)
        {
            return CreateAssetSettingsIssue(qualityLevel, name, k_SRPBatcherSettingDescriptor);
        }

        public static void FixSetting(ProjectIssue issue, Action<RenderPipelineAsset> setter)
        {

            int qualityLevel = issue.GetCustomPropertyInt32(0);
            if (qualityLevel == -1)
            {
                setter(GraphicsSettings.defaultRenderPipeline);
                return;
            }

            var initialQualityLevel = QualitySettings.GetQualityLevel();
            QualitySettings.SetQualityLevel(qualityLevel);
            setter(QualitySettings.renderPipeline);
            QualitySettings.SetQualityLevel(initialQualityLevel);

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
