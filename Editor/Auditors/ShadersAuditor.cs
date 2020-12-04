using System;
using System.Collections.Generic;
using System.Linq;
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
                var issue = new ProjectIssue(descriptor, message, IssueCategory.Shaders);
                issue.SetCustomProperties(new[] { string.Empty, string.Empty });
                onIssueFound(issue);
                onComplete();
                return;
            }

            var shaderPathMap = new Dictionary<string, string>();
            var shaderGuids = AssetDatabase.FindAssets("t:shader");
            foreach (var guid in shaderGuids)
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(guid);
                var shader = AssetDatabase.LoadMainAssetAtPath(assetPath) as Shader;

                shaderPathMap.Add(shader.name, assetPath);
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
                if (shaderPathMap.ContainsKey(shaderName))
                {
                    assetPath = shaderPathMap[shaderName];
                }
                else
                {
                    // built-in shader
                    assetPath = AssetDatabase.GetAssetPath(shader);
                }

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

                    var issue = new ProjectIssue(descriptor, shaderName, IssueCategory.Shaders, new Location(assetPath));
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
