using System;
using System.Collections.Generic;
using Unity.ProjectAuditor.Editor.Diagnostic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace Unity.ProjectAuditor.Editor.SettingsAnalysis
{
    public static class RenderPipelineUtils
    {
        public static List<T> GetAllComponents<T>()
        {
            var allComponents = new List<T>();
            for (int n = 0; n < SceneManager.sceneCount; ++n)
            {
                var scene = SceneManager.GetSceneAt(n);
                var roots = scene.GetRootGameObjects();
                foreach (var go in roots)
                {
                    GetComponents(go, ref allComponents);
                }
            }

            return allComponents;
        }

        private static void GetComponents<T>(GameObject go, ref List<T> components)
        {
            T comp = go.GetComponent<T>();
            if (comp != null)
                components.Add(comp);
            for (int i = 0; i < go.transform.childCount; i++)
            {
                GetComponents(go.transform.GetChild(i).gameObject, ref components);
            }
        }

#if UNITY_2019_3_OR_NEWER
        public static IEnumerable<ProjectIssue> AnalyzeAssets(
            Func<RenderPipelineAsset, int, IEnumerable<ProjectIssue>> analyze)
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

        public static ProjectIssue CreateAssetSettingIssue(int qualityLevel, string name, Descriptor descriptor)
        {
            string assetLocation = qualityLevel == -1
                ? "Default Rendering Pipeline Asset"
                : $"Rendering Pipeline Asset on Quality Level: '{QualitySettings.names[qualityLevel]}'";
            return ProjectIssue.Create(IssueCategory.ProjectSetting, descriptor,
                    name, assetLocation)
                .WithCustomProperties(new object[] { qualityLevel })
                .WithLocation(qualityLevel == -1 ? "Project/Graphics" : "Project/Quality");
        }

        public static void FixAssetSetting(ProjectIssue issue, Action<RenderPipelineAsset> setter)
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
#endif
    }
}
