using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Unity.ProjectAuditor.Editor.Utils;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;

namespace Unity.ProjectAuditor.Editor.Auditors
{
    class ShaderVariantData
    {
        public string passName;
        public ShaderCompilerData compilerData;
    }

    public class ShadersAuditor : IAuditor
#if UNITY_2018_2_OR_NEWER
        , IPreprocessShaders
        , IPreprocessBuildWithReport
#endif
    {
        const int k_ShaderVariantFirstId = 400000;

        static Dictionary<Shader, List<ShaderVariantData>> s_ShaderVariantData;

        public IEnumerable<ProblemDescriptor> GetDescriptors()
        {
            yield return null;
        }

        public void Initialize(ProjectAuditorConfig config)
        {
        }

        public void Reload(string path)
        {
        }

        public void RegisterDescriptor(ProblemDescriptor descriptor)
        {
        }

        public void Audit(Action<ProjectIssue> onIssueFound, Action onComplete, IProgressBar progressBar = null)
        {
            var shaderUtilType = typeof(ShaderUtil);
            var getShaderVariantCountMethod = shaderUtilType.GetMethod("GetVariantCount", BindingFlags.Static | BindingFlags.NonPublic);
            var getShaderGlobalKeywordsMethod = shaderUtilType.GetMethod("GetShaderGlobalKeywords", BindingFlags.Static | BindingFlags.NonPublic);
            var getShaderLocalKeywordsMethod = shaderUtilType.GetMethod("GetShaderLocalKeywords", BindingFlags.Static | BindingFlags.NonPublic);
            var hasInstancingMethod = shaderUtilType.GetMethod("HasInstancing", BindingFlags.Static | BindingFlags.NonPublic);

            var id = k_ShaderVariantFirstId;
            if (s_ShaderVariantData == null)
            {
                var descriptor = new ProblemDescriptor
                    (
                    id,
                    "Shader analysis incomplete",
                    Area.BuildSize,
                    string.Empty,
                    string.Empty
                    );

                var message = "Build the project and run Project Auditor analysis";
#if !UNITY_2018_2_OR_NEWER
                message = "This feature requires Unity 2018";
#endif
                var issue = new ProjectIssue(descriptor, message, IssueCategory.ShaderVariants);
                issue.SetCustomProperties(new[] { string.Empty, string.Empty });
                onIssueFound(issue);
            }

            var shaderGuids = AssetDatabase.FindAssets("t:shader");
            foreach (var guid in shaderGuids)
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(guid);

                // skip editor shaders
                if (assetPath.IndexOf("/editor/", StringComparison.OrdinalIgnoreCase) != -1)
                {
                    continue;
                }

                // vfx shaders are not currently supported
                if (Path.HasExtension(assetPath) && Path.GetExtension(assetPath).Equals(".vfx"))
                {
                    continue;
                }

                var shader = AssetDatabase.LoadMainAssetAtPath(assetPath) as Shader;
                if (shader == null)
                List<ShaderCompilerData> shaderCompilerDataContainer;
            }
                if (shaderCompilerDataContainer != null)
            {
                }
                var shaderName = shader.name;
                var descriptor = new ProblemDescriptor
                    (
                    id++,
                    shaderName,
                    Area.BuildSize,
                    string.Empty,
                    string.Empty
                    );

                var variantCount = (ulong)getShaderVariantCountMethod.Invoke(null, new object[] { shader, usedBySceneOnly});
                var globalKeywords = (string[])getShaderGlobalKeywordsMethod.Invoke(null, new object[] { shader});
                var localKeywords = (string[])getShaderLocalKeywordsMethod.Invoke(null, new object[] { shader});
                var hasInstancing = (bool)hasInstancingMethod.Invoke(null, new object[] { shader});
                string assetPath;
                var issue = new ProjectIssue(descriptor, shader.name, IssueCategory.Shaders, new Location(assetPath));
                {
                    shader.passCount.ToString(),
                    (globalKeywords.Length + localKeywords.Length).ToString(),
                    shader.renderQueue.ToString(),
                    hasInstancing ? "Yes" : "No",
                else
                onIssueFound(issue);

                if (s_ShaderCompilerData != null)
                {
                    List<ShaderCompilerData> shaderCompilerDataContainer;
                    if (shaderCompilerDataContainer != null)

                foreach (var shaderVariantData in shaderVariants)
                    {
                    var compilerData = shaderVariantData.compilerData;
                    var shaderKeywordSet = compilerData.shaderKeywordSet.GetShaderKeywords().ToArray();

#if UNITY_2019_3_OR_NEWER
                        var keywords = shaderKeywordSet.Select(keyword => ShaderKeyword.IsKeywordLocal(keyword) ?  ShaderKeyword.GetKeywordName(shader, keyword) : ShaderKeyword.GetGlobalKeywordName(keyword)).ToArray();
#else
                        var keywords = shaderKeywordSet.Select(keyword => keyword.GetKeywordName()).ToArray();
#endif
                        var keywordString = String.Join(", ", keywords);
                        if (string.IsNullOrEmpty(keywordString))
                            keywordString = "<no keywords>";

                        var issue = new ProjectIssue(descriptor, shader.name, IssueCategory.Shaders, new Location(assetPath));
                        issue.SetCustomProperties(new[]
                        {
                        compilerData.shaderCompilerPlatform.ToString(),
                        shaderVariantData.passName,
                            keywordString,
                        });

                        onIssueFound(issue);
                        }
                }
            }

            onComplete();
        }

void AddShader(Shader shader, string assetPath, int id, Action<ProjectIssue> onIssueFound)
        {
            // skip editor shaders
            if (assetPath.IndexOf("/editor/", StringComparison.OrdinalIgnoreCase) != -1)
                return;

            // vfx shaders are not currently supported
            if (Path.HasExtension(assetPath) && Path.GetExtension(assetPath).Equals(".vfx"))
                return;

            var shaderName = shader.name;
            var descriptor = new ProblemDescriptor
                (
                id++,
                shaderName,
                Area.BuildSize,
                string.Empty,
                string.Empty
                );

            var usedBySceneOnly = false;
            var variantCount = (ulong)getShaderVariantCountMethod.Invoke(null, new object[] { shader, usedBySceneOnly});
            var globalKeywords = (string[])getShaderGlobalKeywordsMethod.Invoke(null, new object[] { shader});
            var localKeywords = (string[])getShaderLocalKeywordsMethod.Invoke(null, new object[] { shader});
            var hasInstancing = (bool)hasInstancingMethod.Invoke(null, new object[] { shader});

            var issue = new ProjectIssue(descriptor, shader.name, IssueCategory.Shaders, new Location(assetPath));
            issue.SetCustomProperties(new[]
            {
                variantCount.ToString(),
                shader.passCount.ToString(),
                (globalKeywords.Length + localKeywords.Length).ToString(),
                shader.renderQueue.ToString(),
                hasInstancing ? "Yes" : "No",
            });
            onIssueFound(issue);
        }

#if UNITY_2018_1_OR_NEWER
        public int callbackOrder { get { return 0; } }
        public void OnPreprocessBuild(BuildReport report)
        {
            s_ShaderVariantData = new Dictionary<Shader, List<ShaderVariantData>>();
        }

        public void OnProcessShader(Shader shader, ShaderSnippetData snippet, IList<ShaderCompilerData> data)
        {
            if (snippet.shaderType != ShaderType.Fragment)
                return;

            if (!s_ShaderVariantData.ContainsKey(shader))
            {
                s_ShaderVariantData.Add(shader, new List<ShaderVariantData>());
            }

            foreach (var shaderCompilerData in data)
            {
                s_ShaderVariantData[shader].Add(new ShaderVariantData
                {
                    passName =  snippet.passName,
                    compilerData = shaderCompilerData
                });
            }
        }

#endif
    }
}
