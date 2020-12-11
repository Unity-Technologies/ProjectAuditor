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

        Type m_ShaderUtilType;
        MethodInfo m_GetShaderVariantCountMethod;
        MethodInfo m_GetShaderGlobalKeywordsMethod;
        MethodInfo m_GetShaderLocalKeywordsMethod;
        MethodInfo m_HasInstancingMethod;

        public IEnumerable<ProblemDescriptor> GetDescriptors()
        {
            yield return null;
        }

        public void Initialize(ProjectAuditorConfig config)
        {
            m_ShaderUtilType = typeof(ShaderUtil);
            m_GetShaderVariantCountMethod = m_ShaderUtilType.GetMethod("GetVariantCount", BindingFlags.Static | BindingFlags.NonPublic);
            m_GetShaderGlobalKeywordsMethod = m_ShaderUtilType.GetMethod("GetShaderGlobalKeywords", BindingFlags.Static | BindingFlags.NonPublic);
            m_GetShaderLocalKeywordsMethod = m_ShaderUtilType.GetMethod("GetShaderLocalKeywords", BindingFlags.Static | BindingFlags.NonPublic);
            m_HasInstancingMethod = m_ShaderUtilType.GetMethod("HasInstancing", BindingFlags.Static | BindingFlags.NonPublic);
        }

        public void Reload(string path)
        {
        }

        public void RegisterDescriptor(ProblemDescriptor descriptor)
        {
        }

        public void Audit(Action<ProjectIssue> onIssueFound, Action onComplete, IProgressBar progressBar = null)
        {
            var shaderPathMap = new Dictionary<Shader, string>();
            var shaderGuids = AssetDatabase.FindAssets("t:shader");
            foreach (var guid in shaderGuids)
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(guid);
                var shader = AssetDatabase.LoadMainAssetAtPath(assetPath) as Shader;

                shaderPathMap.Add(shader, assetPath);
            }

            var id = k_ShaderVariantFirstId;
            if (s_ShaderVariantData == null)
            {
                foreach (var keyValuePair in shaderPathMap)
                {
                    var shader = keyValuePair.Key;
                    var assetPath = keyValuePair.Value;

                    var shaderName = shader.name;
                    AddShader(shader, assetPath, id++, onIssueFound);
                }

                var descriptor = new ProblemDescriptor
                    (
                    id,
                    "Shader Variants analysis incomplete",
                    Area.BuildSize,
                    string.Empty,
                    string.Empty
                    );

                var message = "Build the project to view the Shader Variants";
#if !UNITY_2018_2_OR_NEWER
                message = "This feature requires Unity 2018";
#endif
                var issue = new ProjectIssue(descriptor, message, IssueCategory.ShaderVariants);
                issue.SetCustomProperties(new[] { string.Empty, string.Empty, string.Empty });
                onIssueFound(issue);

                onComplete();
                return;
            }

            foreach (var keyPair in s_ShaderVariantData)
            {
                var shader = keyPair.Key;
                var shaderVariants = keyPair.Value;
                var shaderName = shader.name;
                var descriptor = new ProblemDescriptor
                    (
                    id++,
                    shaderName,
                    Area.BuildSize,
                    string.Empty,
                    string.Empty
                    );

                string assetPath;
                if (shaderPathMap.ContainsKey(shader))
                {
                    assetPath = shaderPathMap[shader];
                }
                else
                {
                    // built-in shader
                    assetPath = AssetDatabase.GetAssetPath(shader);
                }

                AddShader(shader, assetPath, id++, onIssueFound);

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

                    var issue = new ProjectIssue(descriptor, shaderName, IssueCategory.ShaderVariants, new Location(assetPath));
                    issue.SetCustomProperties(new[]
                    {
                        compilerData.shaderCompilerPlatform.ToString(),
                        shaderVariantData.passName,
                        keywordString,
                    });

                    onIssueFound(issue);
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
            const string NotAvailable = "N/A";
            var variantCount = NotAvailable;
            var passCount = NotAvailable;
            var keywordCount = NotAvailable;
            var hasInstancing = NotAvailable;

            if (m_GetShaderVariantCountMethod != null)
            {
                var value = (ulong)m_GetShaderVariantCountMethod.Invoke(null, new object[] { shader, usedBySceneOnly});
                variantCount = value.ToString();
            }

            if (m_GetShaderGlobalKeywordsMethod != null && m_GetShaderLocalKeywordsMethod != null)
            {
                var globalKeywords = (string[])m_GetShaderGlobalKeywordsMethod.Invoke(null, new object[] { shader});
                var localKeywords = (string[])m_GetShaderLocalKeywordsMethod.Invoke(null, new object[] { shader});
                keywordCount = (globalKeywords.Length + localKeywords.Length).ToString();
            }

            if (m_HasInstancingMethod != null)
            {
                var value = (bool)m_HasInstancingMethod.Invoke(null, new object[] { shader});
                hasInstancing = value ? "Yes" : "No";
            }

#if UNITY_2019_1_OR_NEWER
            passCount = shader.passCount.ToString();
#endif
            var issue = new ProjectIssue(descriptor, shader.name, IssueCategory.Shaders, new Location(assetPath));
            issue.SetCustomProperties(new[]
            {
                variantCount,
                passCount,
                keywordCount,
                shader.renderQueue.ToString(),
                hasInstancing,
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
