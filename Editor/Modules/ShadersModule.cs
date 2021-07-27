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
using Object = System.Object;

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

    enum ParseLogResult
    {
        Success,
        NoCompiledVariants,
        ReadError
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

    class ShadersModule : ProjectAuditorModule
#if UNITY_2018_2_OR_NEWER
        , IPreprocessShaders
#endif
    {
        static readonly IssueLayout k_ShaderLayout = new IssueLayout
        {
            category = IssueCategory.Shader,
            properties = new[]
            {
//                new PropertyDefinition { type = PropertyType.Severity},
                new PropertyDefinition { type = PropertyType.Description, name = "Shader Name"},
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(ShaderProperty.NumVariants), format = PropertyFormat.Integer, name = "Actual Variants", longName = "Number of variants in the build" },
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(ShaderProperty.NumPasses), format = PropertyFormat.Integer, name = "Passes", longName = "Number of Passes" },
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(ShaderProperty.NumKeywords), format = PropertyFormat.Integer, name = "Keywords", longName = "Number of Keywords" },
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(ShaderProperty.RenderQueue), format = PropertyFormat.Integer, name = "Render Queue" },
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(ShaderProperty.Instancing), format = PropertyFormat.Bool, name = "Instancing", longName = "GPU Instancing Support" },
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(ShaderProperty.SrpBatcher), format = PropertyFormat.Bool, name = "SRP Batcher", longName = "SRP Batcher Compatible" }
            }
        };

        static readonly IssueLayout k_ShaderVariantLayout = new IssueLayout
        {
            category = IssueCategory.ShaderVariant,
            properties = new[]
            {
                new PropertyDefinition { type = PropertyType.Description, name = "Shader Name"},
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(ShaderVariantProperty.Compiled), format = PropertyFormat.Bool, name = "Compiled", longName = "Compiled at runtime by the player" },
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(ShaderVariantProperty.Platform), format = PropertyFormat.String, name = "Graphics API" },
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(ShaderVariantProperty.PassName), format = PropertyFormat.String, name = "Pass Name" },
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(ShaderVariantProperty.Keywords), format = PropertyFormat.String, name = "Keywords" },
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(ShaderVariantProperty.Requirements), format = PropertyFormat.String, name = "Requirements" }
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

        static Dictionary<Shader, List<ShaderVariantData>> s_ShaderVariantData = new Dictionary<Shader, List<ShaderVariantData>>();

        public override IEnumerable<ProblemDescriptor> GetDescriptors()
        {
            yield return null;
        }

        public override IEnumerable<IssueLayout> GetLayouts()
        {
            yield return k_ShaderLayout;
            yield return k_ShaderVariantLayout;
        }

        public override void Audit(Action<ProjectIssue> onIssueFound, Action onComplete = null, IProgress progress = null)
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

            var sortedShaders = shaderPathMap.Keys.ToList().OrderBy(shader => shader.name);
            foreach (var shader in sortedShaders)
            {
                var assetPath = shaderPathMap[shader];

                AddShader(shader, assetPath, id++, onIssueFound);
            }

            if (onComplete != null)
                onComplete();
        }

        void AddShader(Shader shader, string assetPath, int id, Action<ProjectIssue> onIssueFound)
        {
            // set initial state (-1: info not available)
            var variantCount = s_ShaderVariantData.Count > 0 ? 0 : -1;

#if UNITY_2018_2_OR_NEWER
            // add variants first
            if (s_ShaderVariantData.ContainsKey(shader))
            {
                var variants = s_ShaderVariantData[shader];
                variantCount = variants.Count;

                AddVariants(shader, assetPath, id++, variants, onIssueFound);
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

                var issueWithError = new ProjectIssue(k_ParseErrorDescriptor, shaderName, IssueCategory.Shader, new Location(assetPath));
                issueWithError.SetCustomProperties((int)ShaderProperty.Num, k_NotAvailable);

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
            var issue = new ProjectIssue(descriptor, shaderName, IssueCategory.Shader, new Location(assetPath));
            issue.SetCustomProperties(new object[(int)ShaderProperty.Num]
            {
                variantCount == -1 ? k_NotAvailable : variantCount.ToString(),
                passCount == -1 ? k_NotAvailable : passCount.ToString(),
                (globalKeywords == null || localKeywords == null) ? k_NotAvailable : (globalKeywords.Length + localKeywords.Length).ToString(),
                shader.renderQueue,
                hasInstancing,
                isSrpBatcherCompatible
            });
            onIssueFound(issue);
        }

        public static bool BuildDataAvailable()
        {
            return s_ShaderVariantData.Any();
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
                var issue = new ProjectIssue(descriptor, shaderName, IssueCategory.ShaderVariant, new Location(assetPath));
                issue.SetCustomProperties(new object[(int)ShaderVariantProperty.Num]
                {
                    k_NoRuntimeData,
                    compilerData.shaderCompilerPlatform,
                    shaderVariantData.passName,
                    KeywordsToString(keywords),
                    compilerData.shaderRequirements
                });

                onIssueFound(issue);
            }
        }

        internal static void ClearBuildData()
        {
            s_ShaderVariantData.Clear();
        }

        public int callbackOrder { get { return Int32.MaxValue; } }
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


        public static ParseLogResult ParsePlayerLog(string logFile, ProjectIssue[] builtVariants, IProgress progress = null)
        {
            var compiledVariants = new Dictionary<string, List<CompiledVariantData>>();
            var lines = GetCompiledShaderLines(logFile);
            if (lines == null)
                return ParseLogResult.ReadError;

            foreach (var line in lines)
            {
                var parts = line.Split(new[] {", pass: ", ", stage: ", ", keywords "}, StringSplitOptions.None);
                if (parts.Length != 4)
                {
                    Debug.LogError("Malformed shader compilation log info: " + line);
                    continue;
                }

                var shaderName = parts[0];
                var pass = parts[1];
                var stage = parts[2];
                var keywordsString = parts[3];
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
                return ParseLogResult.NoCompiledVariants;

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
                    builtVariant.SetCustomProperty(ShaderVariantProperty.Compiled, "?");
                    continue;
                }

                var shaderName = shader.name;
                var passName = builtVariant.GetCustomProperty(ShaderVariantProperty.PassName);
                var keywordsString = builtVariant.GetCustomProperty(ShaderVariantProperty.Keywords);
                var keywords = StringToKeywords(keywordsString);
                var isVariantCompiled = false;

                if (compiledVariants.ContainsKey(shaderName))
                {
                    // note that we are not checking pass name since there is an inconsistency regarding "unnamed" passes between build vs compiled
                    var matchingVariants = compiledVariants[shaderName].Where(cv => ShaderVariantsMatch(cv, keywords, passName));
                    isVariantCompiled = matchingVariants.Count() > 0;
                }

                builtVariant.SetCustomProperty(ShaderVariantProperty.Compiled, isVariantCompiled);
            }

            return ParseLogResult.Success;
        }

        static string[] GetCompiledShaderLines(string logFile)
        {
            var compilationLines = new List<string>();
            try
            {
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
            catch (Exception e)
            {
                Debug.Log(e);
                return null;
            }
        }

        static bool ShaderVariantsMatch(CompiledVariantData cv, string[] secondSet, string passName)
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

#if UNITY_2018_2_OR_NEWER
        static string[] GetShaderKeywords(Shader shader, ShaderKeyword[] shaderKeywords)
        {
#if UNITY_2019_3_OR_NEWER
            var keywords = shaderKeywords.Select(keyword => ShaderKeyword.IsKeywordLocal(keyword) ? ShaderKeyword.GetKeywordName(shader, keyword) : ShaderKeyword.GetGlobalKeywordName(keyword)).ToArray();
#else
            var keywords = shaderKeywords.Select(keyword => keyword.GetKeywordName()).ToArray();
#endif
            return keywords;
        }

#endif

        static string[] StringToKeywords(string keywordsString)
        {
            if (keywordsString.Equals(k_NoKeywords))
                return new string[] {};
            return keywordsString.Split(' ');
        }

        static string KeywordsToString(string[] keywords)
        {
            var keywordString = String.Join(" ", keywords);
            if (string.IsNullOrEmpty(keywordString))
                keywordString = k_NoKeywords;
            return keywordString;
        }
    }
}
