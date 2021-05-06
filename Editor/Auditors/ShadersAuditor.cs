#if UNITY_2018_2_OR_NEWER
using UnityEditor.Build.Reporting;
#endif
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.ProjectAuditor.Editor.Utils;
using UnityEditor;
using UnityEditor.Build;
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
        SrpBatcher,
        Num
    }

    public enum ShaderKeywordProperty
    {
        NumShaders = 0,
        NumVariants,
        BuildSize,
        Num
    }

    public enum ShaderVariantProperty
    {
        Compiled = 0,
        Platform,
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

    class CompiledVariantData
    {
        public string pass;
        public string stage;
        public string[] keywords;
    }

    public class ShadersAuditor : IAuditor
#if UNITY_2018_2_OR_NEWER
        , IPreprocessShaders
        , IPreprocessBuildWithReport
#endif
    {
        static readonly IssueLayout k_ShaderLayout = new IssueLayout
        {
            category = IssueCategory.Shaders,
            properties = new[]
            {
//                new PropertyDefinition { type = PropertyType.Severity},
                new PropertyDefinition { type = PropertyType.Description, name = "Shader Name"},
                new PropertyDefinition { type = PropertyType.Custom, format = PropertyFormat.Integer, name = "Actual Variants", longName = "Number of variants in the build" },
                new PropertyDefinition { type = PropertyType.Custom + 1, format = PropertyFormat.Integer, name = "Passes", longName = "Number of Passes" },
                new PropertyDefinition { type = PropertyType.Custom + 2, format = PropertyFormat.Integer, name = "Keywords", longName = "Number of Keywords" },
                new PropertyDefinition { type = PropertyType.Custom + 3, format = PropertyFormat.Integer, name = "Render Queue" },
                new PropertyDefinition { type = PropertyType.Custom + 4, format = PropertyFormat.Bool, name = "Instancing", longName = "GPU Instancing Support" },
                new PropertyDefinition { type = PropertyType.Custom + 5, format = PropertyFormat.Bool, name = "SRP Batcher", longName = "SRP Batcher Compatible" }
            }
        };

        static readonly IssueLayout k_ShaderVariantLayout = new IssueLayout
        {
            category = IssueCategory.ShaderVariants,
            properties = new[]
            {
                new PropertyDefinition { type = PropertyType.Description, name = "Shader Name"},
                new PropertyDefinition { type = PropertyType.Custom, format = PropertyFormat.Bool, name = "Compiled", longName = "Compiled at runtime by the player" },
                new PropertyDefinition { type = PropertyType.Custom + 1, format = PropertyFormat.String, name = "Graphics API" },
                new PropertyDefinition { type = PropertyType.Custom + 2, format = PropertyFormat.String, name = "Pass Name" },
                new PropertyDefinition { type = PropertyType.Custom + 3, format = PropertyFormat.String, name = "Keywords" },
                new PropertyDefinition { type = PropertyType.Custom + 4, format = PropertyFormat.String, name = "Requirements" }
            }
        };

        static readonly ProblemDescriptor k_ParseErrorDescriptor = new ProblemDescriptor
            (
            400000,
            "Parse Error"
            )
        {
            severity = Rule.Severity.Error
        };

        internal const string k_NoPassName = "<unnamed>";
        internal const string k_UnamedPassPrefix = "Pass ";
        internal const string k_NoKeywords = "<no keywords>";
        internal const string k_NoRuntimeData = "?";
        internal const string k_NotAvailable = "N/A";
        const int k_ShaderVariantFirstId = 400001;

        static Dictionary<Shader, List<ShaderVariantData>> s_ShaderVariantData;

        public IEnumerable<ProblemDescriptor> GetDescriptors()
        {
            yield return null;
        }

        public IEnumerable<IssueLayout> GetLayouts()
        {
            yield return k_ShaderLayout;
            yield return k_ShaderVariantLayout;
        }

        public void Initialize(ProjectAuditorConfig config)
        {
        }

        public bool IsSupported()
        {
            return true;
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
                if (assetPath.IndexOf("/editor default resources/", StringComparison.OrdinalIgnoreCase) != -1)
                    continue;

                // vfx shaders are not currently supported
                if (Path.HasExtension(assetPath) && Path.GetExtension(assetPath).Equals(".vfx"))
                    continue;

                var shader = AssetDatabase.LoadMainAssetAtPath(assetPath) as Shader;
                if (shader == null)
                {
                    Debug.LogError(assetPath + " is not a Shader.");
                    continue;
                }

                shaderPathMap.Add(shader, assetPath);
            }

            var id = k_ShaderVariantFirstId;
            if (s_ShaderVariantData != null)
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
            var variantCount = -1; // initial state: info not available

#if UNITY_2018_2_OR_NEWER
            // add variants first
            if (s_ShaderVariantData != null)
                if (s_ShaderVariantData.ContainsKey(shader))
                {
                    var variants = s_ShaderVariantData[shader];
                    variantCount = variants.Count;

                    AddVariants(shader, assetPath, id++, variants, onIssueFound);
                }
                else
                {
                    variantCount = 0;
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
                    k_NotAvailable
                });
                onIssueFound(issueWithError);

                return;
            }

            var descriptor = new ProblemDescriptor
                (
                id++,
                shaderName
                );

/*
            var usedBySceneOnly = false;
            if (m_GetShaderVariantCountMethod != null)
            {
                var value = (ulong)m_GetShaderVariantCountMethod.Invoke(null, new object[] { shader, usedBySceneOnly});
                variantCount = value.ToString();
            }
*/
            var passCount = -1;
            var globalKeywords = ShaderUtilProxy.GetShaderGlobalKeywords(shader);
            var localKeywords = ShaderUtilProxy.GetShaderLocalKeywords(shader);
            var hasInstancing = ShaderUtilProxy.HasInstancing(shader);
            var subShaderIndex = ShaderUtilProxy.GetShaderActiveSubshaderIndex(shader);
            var isSrpBatcherCompatible = ShaderUtilProxy.GetSRPBatcherCompatibilityCode(shader, subShaderIndex) == 0;

#if UNITY_2019_1_OR_NEWER
            passCount = shader.passCount;
#endif
            var issue = new ProjectIssue(descriptor, shaderName, IssueCategory.Shaders, new Location(assetPath));
            issue.SetCustomProperties(new[]
            {
                variantCount == -1 ? k_NotAvailable : variantCount.ToString(),
                passCount == -1 ? k_NotAvailable : passCount.ToString(),
                (globalKeywords == null || localKeywords == null) ? k_NotAvailable : (globalKeywords.Length + localKeywords.Length).ToString(),
                shader.renderQueue.ToString(),
                hasInstancing.ToString(),
                isSrpBatcherCompatible.ToString()
            });
            onIssueFound(issue);
        }

        public static bool BuildDataAvailable()
        {
            return s_ShaderVariantData != null;
        }

#if UNITY_2018_2_OR_NEWER
        void AddVariants(Shader shader, string assetPath, int id, List<ShaderVariantData> shaderVariants, Action<ProjectIssue> onIssueFound)
        {
            var shaderName = shader.name;
            var descriptor = new ProblemDescriptor
                (
                id++,
                shaderName
                );

            foreach (var shaderVariantData in shaderVariants)
            {
                var compilerData = shaderVariantData.compilerData;
                var keywords = GetShaderKeywords(shader, compilerData.shaderKeywordSet.GetShaderKeywords());
                var issue = new ProjectIssue(descriptor, shaderName, IssueCategory.ShaderVariants, new Location(assetPath));
                issue.SetCustomProperties(new[]
                {
                    k_NoRuntimeData,
                    compilerData.shaderCompilerPlatform.ToString(),
                    shaderVariantData.passName,
                    KeywordsToString(keywords),
                    compilerData.shaderRequirements.ToString()
                });

                onIssueFound(issue);
            }
        }

        internal static void ClearBuildData()
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


        public bool ParsePlayerLog(string logFile, ProjectIssue[] builtVariants, IProgressBar progressBar = null)
        {
            var compiledVariants = new Dictionary<string, List<CompiledVariantData>>();
            var lines = GetCompiledShaderLines(logFile);
            foreach (var line in lines)
            {
                var parts = line.Split(',');
                var shaderName = parts[0];
                var pass = parts[1].Trim(' ').Substring("pass: ".Length);
                var stage = parts[2].Trim(' ').Substring("stage: ".Length);
                var keywordsString = parts[3].Trim(' ').Substring("keywords ".Length); // note that the log is missing ':'
                var keywords = StringToKeywords(keywordsString);

                if (!stage.Equals("fragment") && !stage.Equals("pixel") && !stage.Equals("all"))
                    continue;

                if (!compiledVariants.ContainsKey(shaderName))
                {
                    compiledVariants.Add(shaderName, new List<CompiledVariantData>());
                }
                compiledVariants[shaderName].Add(new CompiledVariantData
                {
                    pass = pass,
                    stage = stage,
                    keywords = keywords
                });
            }

            if (!compiledVariants.Any())
                return false;

            builtVariants = builtVariants.OrderBy(v => v.description).ToArray();
            var shader = (Shader)null;
            foreach (var builtVariant in builtVariants)
            {
                if (shader == null || !shader.name.Equals(builtVariant.description))
                {
                    shader = Shader.Find(builtVariant.description);
                }

                if (shader == null)
                {
                    builtVariant.SetCustomProperty((int)ShaderVariantProperty.Compiled, "?");
                    continue;
                }

                var shaderName = shader.name;
                var passName = builtVariant.GetCustomProperty((int)ShaderVariantProperty.PassName);
                var keywordsString = builtVariant.GetCustomProperty((int)ShaderVariantProperty.Keywords);
                var keywords = StringToKeywords(keywordsString);
                var isVariantCompiled = false;

                if (compiledVariants.ContainsKey(shaderName))
                {
                    // note that we are not checking pass name since there is an inconsistency regarding "unnamed" passes between build vs compiled
                    var matchingVariants = compiledVariants[shaderName].Where(cv => ShaderVariantsMatch(cv, keywords, passName));
                    isVariantCompiled = matchingVariants.Count() > 0;
                }

                builtVariant.SetCustomProperty((int)ShaderVariantProperty.Compiled, isVariantCompiled.ToString());
            }

            return true;
        }

        string[] GetCompiledShaderLines(string logFile)
        {
            var compilationLines = new List<string>();
            using (var file = new StreamReader(logFile))
            {
                string line;
                while ((line = file.ReadLine()) != null)
                {
                    const string prefix = "Compiled shader: ";
                    var compilationLogIndex = line.IndexOf(prefix);
                    if (compilationLogIndex >= 0)
                        compilationLines.Add(line.Substring(compilationLogIndex + prefix.Length));
                }
            }
            return compilationLines.ToArray();
        }

#if UNITY_2018_2_OR_NEWER
        string[] GetShaderKeywords(Shader shader, ShaderKeyword[] shaderKeywords)
        {
#if UNITY_2019_3_OR_NEWER
            var keywords = shaderKeywords.Select(keyword => ShaderKeyword.IsKeywordLocal(keyword) ? ShaderKeyword.GetKeywordName(shader, keyword) : ShaderKeyword.GetGlobalKeywordName(keyword)).ToArray();
#else
            var keywords = shaderKeywords.Select(keyword => keyword.GetKeywordName()).ToArray();
#endif
            return keywords;
        }

#endif

        bool ShaderVariantsMatch(CompiledVariantData cv, string[] secondSet, string passName)
        {
            var passMatch = cv.pass.Equals(passName);
            if (!passMatch)
            {
#if UNITY_2019_1_OR_NEWER
                var pass = 0;
                passMatch = cv.pass.Equals(k_NoPassName) && passName.StartsWith(k_UnamedPassPrefix) && int.TryParse(passName.Substring(k_UnamedPassPrefix.Length), out pass);
#else
                passMatch = cv.pass.Equals(k_NoPassName) && string.IsNullOrEmpty(passName);
#endif
            }

            if (!passMatch)
                return false;
            return cv.keywords.OrderBy(e => e).SequenceEqual(secondSet.OrderBy(e => e));
        }

        string[] StringToKeywords(string keywordsString)
        {
            if (keywordsString.Equals(k_NoKeywords))
                return new string[] {};
            return keywordsString.Split(' ');
        }

        string KeywordsToString(string[] keywords)
        {
            var keywordString = String.Join(" ", keywords);
            if (string.IsNullOrEmpty(keywordString))
                keywordString = k_NoKeywords;
            return keywordString;
        }
    }
}
