using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Unity.ProjectAuditor.Editor.Utils;
using UnityEditor;
using UnityEditor.Build;
#if UNITY_2018_2_OR_NEWER
using UnityEditor.Build.Reporting;
#endif
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;

namespace Unity.ProjectAuditor.Editor.Auditors
{
    public enum ShaderProperty
    {
        NumVariants = 0,
        NumPasses,
        NumKeywords,
        RenderQueue,
        Instancing,
        Num
    }

    public enum ShaderVariantProperty
    {
        Platform = 0,
        PassName,
        Keywords,
        Requirements,
        Num
    }

    class ShaderVariantData
    {
        public string passName;
#if UNITY_2018_2_OR_NEWER
        public ShaderCompilerData compilerData;
#endif
    }

    public class ShadersAuditor : IAuditor
#if UNITY_2018_2_OR_NEWER
        , IPreprocessShaders
        , IPreprocessBuildWithReport
#endif
    {
        ProblemDescriptor k_ParseErrorDescriptor = new ProblemDescriptor
            (
            400000,
            "Parse Error",
            Area.BuildSize,
            string.Empty,
            string.Empty
            )
        {
            severity = Rule.Severity.Error
        };

        ProblemDescriptor k_BuildRequiredDescriptor = new ProblemDescriptor
            (
            400001,
            "Shader Variants analysis incomplete",
            Area.BuildSize,
            string.Empty,
            string.Empty
            )
        {
            severity = Rule.Severity.Error
        };

        ProblemDescriptor k_RuntimeCompiledShaderVariantDescriptor = new ProblemDescriptor
            (
            400002,
            "Runtime-Compiled Shader Variant",
            Area.CPU,
            string.Empty,
            string.Empty
            );

        const string k_NotAvailable = "N/A";
        const int k_ShaderVariantFirstId = 400003;

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

                // skip editor shaders
                if (assetPath.IndexOf("/editor/", StringComparison.OrdinalIgnoreCase) != -1)
                    continue;

                // vfx shaders are not currently supported
                if (Path.HasExtension(assetPath) && Path.GetExtension(assetPath).Equals(".vfx"))
                    continue;

                var shader = AssetDatabase.LoadMainAssetAtPath(assetPath) as Shader;

                shaderPathMap.Add(shader, assetPath);
            }

            var id = k_ShaderVariantFirstId;
            if (s_ShaderVariantData == null)
            {
                var message = "Build the project to view the Shader Variants";
#if !UNITY_2018_2_OR_NEWER
                message = "This feature requires Unity 2018.2 or newer";
#endif
                var issue = new ProjectIssue(k_BuildRequiredDescriptor, message, IssueCategory.ShaderVariants);
                issue.SetCustomProperties(new[] { string.Empty, string.Empty, string.Empty, string.Empty });
                onIssueFound(issue);
            }
            else
            {
#if UNITY_2018_2_OR_NEWER
                // find hidden shaders
                var shadersInBuild = s_ShaderVariantData.Select(variant => variant.Key);
                foreach (var shader in shadersInBuild)
                {
                    // skip shader if it's been removed since the last build
                    if (shader == null)
                        continue;

                    if (!shaderPathMap.ContainsKey(shader))
                    {
                        var assetPath = AssetDatabase.GetAssetPath(shader);

                        shaderPathMap.Add(shader, assetPath);
                    }
                }
#endif
            }

            var sortedShaders = shaderPathMap.Keys.ToList().OrderBy(shader => shader.name);
            foreach (var shader in sortedShaders)
            {
                var assetPath = shaderPathMap[shader];

                AddShader(shader, assetPath, id++, onIssueFound);
            }

            onComplete();
        }

        void AddShader(Shader shader, string assetPath, int id, Action<ProjectIssue> onIssueFound)
        {
            var variantCount = k_NotAvailable;

#if UNITY_2018_2_OR_NEWER
            // add variants first
            if (s_ShaderVariantData != null)
                if (s_ShaderVariantData.ContainsKey(shader))
                {
                    var variants = s_ShaderVariantData[shader];
                    variantCount = variants.Count.ToString();

                    AddVariants(shader, assetPath, id++, variants, onIssueFound);
                }
                else
                {
                    variantCount = "0";
                }
#endif

            var shaderName = shader.name;
            var shaderHasError = false;

#if UNITY_2019_4_OR_NEWER
            shaderHasError = ShaderUtil.ShaderHasError(shader);
#endif

            if (shaderHasError)
            {
                shaderName = Path.GetFileNameWithoutExtension(assetPath) + ": Parse Error";

                var issueWithError = new ProjectIssue(k_ParseErrorDescriptor, shaderName, IssueCategory.Shaders, new Location(assetPath));
                issueWithError.SetCustomProperties(new[]
                {
                    k_NotAvailable,
                    k_NotAvailable,
                    k_NotAvailable,
                    k_NotAvailable,
                    k_NotAvailable,
                });
                onIssueFound(issueWithError);

                return;
            }

            var descriptor = new ProblemDescriptor
                (
                id++,
                shaderName,
                Area.BuildSize,
                string.Empty,
                string.Empty
                );

            var passCount = k_NotAvailable;
            var keywordCount = k_NotAvailable;
            var hasInstancing = k_NotAvailable;
/*
            var usedBySceneOnly = false;
            if (m_GetShaderVariantCountMethod != null)
            {
                var value = (ulong)m_GetShaderVariantCountMethod.Invoke(null, new object[] { shader, usedBySceneOnly});
                variantCount = value.ToString();
            }
*/
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
            var issue = new ProjectIssue(descriptor, shaderName, IssueCategory.Shaders, new Location(assetPath));
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

#if UNITY_2018_2_OR_NEWER
        void AddVariants(Shader shader, string assetPath, int id, List<ShaderVariantData> shaderVariants, Action<ProjectIssue> onIssueFound)
        {
            var shaderName = shader.name;
            var descriptor = new ProblemDescriptor
                (
                id++,
                shaderName,
                Area.BuildSize,
                string.Empty,
                string.Empty
                );

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
                    compilerData.shaderRequirements.ToString()
                });

                onIssueFound(issue);
            }
        }

        internal static void CleanupBuildData()
        {
            s_ShaderVariantData = null;
        }

        public int callbackOrder { get { return Int32.MaxValue; } }
        public void OnPreprocessBuild(BuildReport report)
        {
            s_ShaderVariantData = new Dictionary<Shader, List<ShaderVariantData>>();
        }

        public void OnProcessShader(Shader shader, ShaderSnippetData snippet, IList<ShaderCompilerData> data)
        {
            if (snippet.shaderType != ShaderType.Fragment)
                return;

            // if s_ShaderVariantData is null, we might be building AssetBundles
            if (s_ShaderVariantData == null)
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
        public void ParsePlayerLog(string logFile, Action<ProjectIssue> onIssueFound, IProgressBar progressBar = null)
        {
            var lines = GetAllLines(logFile);
            foreach (var line in lines)
            {
                var strippedLine = line.Substring("Compiled shader: ".Length);
                var parts = strippedLine.Split(',');
                var shaderName = parts[0];
                var pass = parts[1].Substring("pass: ".Length);
                var stage = parts[2].Substring("stage: ".Length);
                var keywords = parts[3].Substring("keywords: ".Length);
                var issue = new ProjectIssue(k_RuntimeCompiledShaderVariantDescriptor, shaderName, IssueCategory.ShaderCompilationLog);
                issue.SetCustomProperties(new string[] { pass, stage, keywords });
                onIssueFound(issue);
            }
        }

        string[] GetAllLines(string logFile)
        {
            var compilationLines = new List<string>();
            using (var file = new StreamReader(logFile))
            {
                string line;
                while ((line = file.ReadLine()) != null)
                {
                    if (line.StartsWith("Compiled shader:"))
                        compilationLines.Add(line);
                }
            }
            return compilationLines.ToArray();
        }
    }
}
