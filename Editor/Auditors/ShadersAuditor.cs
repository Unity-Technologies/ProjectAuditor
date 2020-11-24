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
    public class ShadersAuditor : IAuditor
#if UNITY_2018_2_OR_NEWER
        , IPreprocessShaders
        , IPreprocessBuildWithReport
#endif
    {
        const int k_ShaderVariantFirstId = 400000;

        static Dictionary<string, List<ShaderCompilerData>> s_ShaderCompilerData;

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
            if (s_ShaderCompilerData == null)
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
                issue.SetCustomProperties(new[] { string.Empty, string.Empty});
                onIssueFound(issue);
                onComplete();
                return;
            }

            var shaderGuids = AssetDatabase.FindAssets("t:shader");
            foreach (var guid in shaderGuids)
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(guid);
                var shader = AssetDatabase.LoadMainAssetAtPath(assetPath) as Shader;

                List<ShaderCompilerData> shaderCompilerDataContainer;
                s_ShaderCompilerData.TryGetValue(shader.name, out shaderCompilerDataContainer);
                if (shaderCompilerDataContainer != null)
                {
                    var descriptor = new ProblemDescriptor
                        (
                        id++,
                        shader.name,
                        Area.BuildSize,
                        string.Empty,
                        string.Empty
                        );

                    foreach (var shaderCompilerData in shaderCompilerDataContainer)
                    {
                        var shaderKeywordSet = shaderCompilerData.shaderKeywordSet.GetShaderKeywords().ToArray();

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
                            shaderCompilerData.shaderCompilerPlatform.ToString(),
                            keywordString,
                        });

                        onIssueFound(issue);
                    }
                }
            }

            onComplete();
        }

#if UNITY_2018_1_OR_NEWER
        public int callbackOrder { get { return 0; } }
        public void OnPreprocessBuild(BuildReport report)
        {
            s_ShaderCompilerData = new Dictionary<string, List<ShaderCompilerData>>();
        }

        public void OnProcessShader(Shader shader, ShaderSnippetData snippet, IList<ShaderCompilerData> data)
        {
            if (snippet.shaderType != ShaderType.Fragment)
                return;

            var shaderName = shader.name;

            if (!s_ShaderCompilerData.ContainsKey(shaderName))
            {
                s_ShaderCompilerData.Add(shaderName, new List<ShaderCompilerData>());
            }
            s_ShaderCompilerData[shaderName].AddRange(data);
        }

#endif
    }
}
