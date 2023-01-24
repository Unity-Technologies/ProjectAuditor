#if UNITY_2020_1_OR_NEWER
    #define COMPUTE_SHADER_ANALYSIS
#endif


using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.ProjectAuditor.Editor.Core;
using Unity.ProjectAuditor.Editor.Diagnostic;
using Unity.ProjectAuditor.Editor.Utils;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;

namespace Unity.ProjectAuditor.Editor.Modules
{
    public enum ShaderProperty
    {
        Size = 0,
        NumVariants,
        NumBuiltVariants,
        NumPasses,
        NumKeywords,
        RenderQueue,
        Instancing,
        SrpBatcher,
        AlwaysIncluded,
        Num
    }

    public enum ShaderVariantProperty
    {
        Compiled = 0,
        Platform,
        Tier,
        Stage,
        PassType,
        PassName,
        Keywords,
        PlatformKeywords,
        Requirements,
        Num
    }

    public enum ComputeShaderVariantProperty
    {
        Platform = 0,
        Tier,
        Kernel,
        Keywords,
        PlatformKeywords,
        Num
    }

    public enum ShaderMessageProperty
    {
        ShaderName = 0,
        Platform,
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
        public PassType passType;
        public string passName;
        public ShaderType shaderType;
        public string[] keywords;
        public string[] platformKeywords;
        public ShaderRequirements[] requirements;
        public GraphicsTier graphicsTier;
        public BuildTarget buildTarget;
        public ShaderCompilerPlatform compilerPlatform;
    }

    class ComputeShaderVariantData
    {
        public string kernelName;
        public string[] keywords;
        public string[] platformKeywords;
        public GraphicsTier graphicsTier;
        public BuildTarget buildTarget;
        public ShaderCompilerPlatform compilerPlatform;
    }

    class CompiledVariantData
    {
        public string pass;
        public string stage;
        public string[] keywords;
    }

    class ShadersModule : ProjectAuditorModule
        , IPreprocessShaders
#if COMPUTE_SHADER_ANALYSIS
        , IPreprocessComputeShaders
#endif
    {
        static readonly IssueLayout k_ShaderLayout = new IssueLayout
        {
            category = IssueCategory.Shader,
            properties = new[]
            {
                new PropertyDefinition { type = PropertyType.LogLevel},
                new PropertyDefinition { type = PropertyType.Description, name = "Shader Name"},
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(ShaderProperty.Size), format = PropertyFormat.Bytes, name = "Size", longName = "Size of the variants in the build" },
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(ShaderProperty.NumVariants), format = PropertyFormat.Integer, name = "Num Variants", longName = "Number of potential shader variants for a single stage (e.g. fragment), per shader platform (e.g. GLES30)" },
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(ShaderProperty.NumBuiltVariants), format = PropertyFormat.Integer, name = "Built Fragment Variants", longName = "Number of fragment shader variants in the build for a single stage (e.g. fragment), per shader platform (e.g. GLES30)" },
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(ShaderProperty.NumPasses), format = PropertyFormat.Integer, name = "Num Passes", longName = "Number of Passes" },
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(ShaderProperty.NumKeywords), format = PropertyFormat.Integer, name = "Num Keywords", longName = "Number of Keywords" },
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(ShaderProperty.RenderQueue), format = PropertyFormat.Integer, name = "Render Queue" },
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(ShaderProperty.Instancing), format = PropertyFormat.Bool, name = "Instancing", longName = "GPU Instancing Support" },
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(ShaderProperty.SrpBatcher), format = PropertyFormat.Bool, name = "SRP Batcher", longName = "SRP Batcher Compatible" },
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(ShaderProperty.AlwaysIncluded), format = PropertyFormat.Bool, name = "Always Included", longName = "Always Included in Build" },
                new PropertyDefinition { type = PropertyType.Path, name = "Source Asset"},
                new PropertyDefinition { type = PropertyType.Directory, name = "Directory", defaultGroup = true}
            }
        };

        static readonly IssueLayout k_ShaderVariantLayout = new IssueLayout
        {
            category = IssueCategory.ShaderVariant,
            properties = new[]
            {
                new PropertyDefinition { type = PropertyType.Description, name = "Shader Name", defaultGroup = true },
                new PropertyDefinition
                {
                    type = PropertyTypeUtil.FromCustom(ShaderVariantProperty.Compiled), format = PropertyFormat.Bool,
                    name = "Compiled", longName = "Compiled at runtime by the player"
                },
                new PropertyDefinition
                {
                    type = PropertyTypeUtil.FromCustom(ShaderVariantProperty.Platform), format = PropertyFormat.String,
                    name = "Graphics API"
                },
                new PropertyDefinition
                {
                    type = PropertyTypeUtil.FromCustom(ShaderVariantProperty.Tier), format = PropertyFormat.String,
                    name = "Tier"
                },
                new PropertyDefinition
                {
                    type = PropertyTypeUtil.FromCustom(ShaderVariantProperty.Stage), format = PropertyFormat.String,
                    name = "Stage"
                },
                new PropertyDefinition
                {
                    type = PropertyTypeUtil.FromCustom(ShaderVariantProperty.PassType), format = PropertyFormat.String,
                    name = "Pass Type"
                },
                new PropertyDefinition
                {
                    type = PropertyTypeUtil.FromCustom(ShaderVariantProperty.PassName), format = PropertyFormat.String,
                    name = "Pass Name"
                },
                new PropertyDefinition
                {
                    type = PropertyTypeUtil.FromCustom(ShaderVariantProperty.Keywords), format = PropertyFormat.String,
                    name = "Keywords"
                },
                new PropertyDefinition
                {
                    type = PropertyTypeUtil.FromCustom(ShaderVariantProperty.PlatformKeywords),
                    format = PropertyFormat.String, name = "Platform Keywords"
                },
                new PropertyDefinition
                {
                    type = PropertyTypeUtil.FromCustom(ShaderVariantProperty.Requirements),
                    format = PropertyFormat.String, name = "Requirements"
                }
            }
        };

        static readonly IssueLayout k_ComputeShaderVariantLayout = new IssueLayout
        {
            category = IssueCategory.ComputeShaderVariant,
            properties = new[]
            {
                new PropertyDefinition { type = PropertyType.Description, name = "Shader Name", defaultGroup = true },
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(ComputeShaderVariantProperty.Platform), format = PropertyFormat.String, name = "Graphics API" },
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(ComputeShaderVariantProperty.Tier), format = PropertyFormat.String, name = "Tier" },
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(ComputeShaderVariantProperty.Kernel), format = PropertyFormat.String, name = "Kernel" },
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(ComputeShaderVariantProperty.Keywords), format = PropertyFormat.String, name = "Keywords" },
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(ComputeShaderVariantProperty.PlatformKeywords), format = PropertyFormat.String, name = "Platform Keywords" },
            }
        };

        static readonly IssueLayout k_ShaderCompilerMessageLayout = new IssueLayout
        {
            category = IssueCategory.ShaderCompilerMessage,
            properties = new[]
            {
                new PropertyDefinition { type = PropertyType.LogLevel},
                new PropertyDefinition { type = PropertyType.Description, name = "Message"},
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(ShaderMessageProperty.ShaderName), format = PropertyFormat.String, name = "Shader Name", defaultGroup = true},
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(ShaderMessageProperty.Platform), format = PropertyFormat.String, name = "Platform"},
                new PropertyDefinition { type = PropertyType.Path, name = "Path"},
            }
        };

        // k_NoPassNames and k_NoKeywords must be consistent with values assigned in SubProgram::Compile()
        internal static readonly string[] k_NoPassNames = new[] { "unnamed", "<unnamed>"}; // 2019.x uses: <unnamed>, whilst 2020.x uses unnamed
        internal static readonly Dictionary<string, string> k_StageNameMap = new Dictionary<string, string>()
        {
            { "all", "vertex" },       // GLES* / OpenGLCore
            { "pixel", "fragment" }    // Metal
        };
        internal const string k_NoKeywords = "<no keywords>";
        internal const string k_UnnamedPassPrefix = "Pass ";
        internal const string k_NoRuntimeData = "This feature requires runtime data.";
        internal const string k_NotAvailable = "This feature is requires a build.";
        internal const string k_Unknown = "Unknown";

        static Dictionary<Shader, List<ShaderVariantData>> s_ShaderVariantData =
            new Dictionary<Shader, List<ShaderVariantData>>();
#if COMPUTE_SHADER_ANALYSIS
        static Dictionary<ComputeShader, List<ComputeShaderVariantData>> s_ComputeShaderVariantData =
            new Dictionary<ComputeShader, List<ComputeShaderVariantData>>();
#endif

        public override string name => "Shaders";

        public override IReadOnlyCollection<IssueLayout> supportedLayouts => new IssueLayout[]
        {
            k_ShaderLayout,
            k_ShaderVariantLayout,

#if COMPUTE_SHADER_ANALYSIS
            k_ComputeShaderVariantLayout,
#endif

#if UNITY_2019_1_OR_NEWER
            k_ShaderCompilerMessageLayout
#endif
        };

        public override void Audit(ProjectAuditorParams projectAuditorParams, IProgress progress = null)
        {
            var shaderPathMap = CollectShaders();
            ProcessShaders(projectAuditorParams.platform, shaderPathMap, projectAuditorParams.onIncomingIssues);

            ProcessComputeShaders(projectAuditorParams.platform, projectAuditorParams.onIncomingIssues);

            // clear collected variants before next build
            ClearBuildData();

            projectAuditorParams.onModuleCompleted?.Invoke();
        }

        Dictionary<Shader, string> CollectShaders()
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

            var builtShaderPaths = GetBuiltShaderPaths();

            foreach (var builtShader in builtShaderPaths)
            {
                if (!shaderPathMap.ContainsKey(builtShader.Key))
                {
                    shaderPathMap.Add(builtShader.Key, builtShader.Value);
                }
            }

            return shaderPathMap;
        }

        static Dictionary<Shader, string> GetBuiltShaderPaths()
        {
            // note this will find hidden shaders too
            return s_ShaderVariantData.Select(variant => variant.Key)
                .Where(shader => shader != null) // skip shader if it's been removed since the last build
                .ToDictionary(s => s, AssetDatabase.GetAssetPath);
        }

        static HashSet<Shader> GetAlwaysIncludedShaders()
        {
            var alwaysIncludedShaders = new HashSet<Shader>();
            var graphicsSettings = Unsupported.GetSerializedAssetInterfaceSingleton("GraphicsSettings");
            var graphicsSettingsSerializedObject = new SerializedObject(graphicsSettings);
            var alwaysIncludedShadersSerializedProperty =
                graphicsSettingsSerializedObject.FindProperty("m_AlwaysIncludedShaders");

            for (var i = 0; i < alwaysIncludedShadersSerializedProperty.arraySize; i++)
            {
                var shader = (Shader)alwaysIncludedShadersSerializedProperty.GetArrayElementAtIndex(i)
                    .objectReferenceValue;

                // sanity check, maybe the shader was removed/deleted
                if (shader == null)
                    continue;

                if (!alwaysIncludedShaders.Contains(shader))
                {
                    alwaysIncludedShaders.Add(shader);
                }
            }

            return alwaysIncludedShaders;
        }

        void ProcessShaders(BuildTarget platform, Dictionary<Shader, string> shaderPathMap,
            Action<IEnumerable<ProjectIssue>> onIncomingIssues)
        {
            var alwaysIncludedShaders = GetAlwaysIncludedShaders();
            var buildReportInfoAvailable = false;
#if BUILD_REPORT_API_SUPPORT
            var packetAssetInfos = new PackedAssetInfo[0];
            var buildReport = BuildReportModule.BuildReportProvider.GetBuildReport(platform);
            if (buildReport != null)
            {
                packetAssetInfos = buildReport.packedAssets.SelectMany(packedAsset => packedAsset.contents)
                    .Where(c => c.type == typeof(UnityEngine.Shader)).ToArray();
            }

            buildReportInfoAvailable = packetAssetInfos.Length > 0;
#endif
            var sortedShaders = shaderPathMap.Keys.ToList().OrderBy(shader => shader.name);
            foreach (var shader in sortedShaders)
            {
                var assetPath = shaderPathMap[shader];
                var assetSize = buildReportInfoAvailable ? k_Unknown : k_NotAvailable;
#if BUILD_REPORT_API_SUPPORT
                if (!assetPath.Equals("Resources/unity_builtin_extra"))
                {
                    var builtAssets = packetAssetInfos.Where(p => p.sourceAssetPath.Equals(assetPath)).ToArray();
                    if (builtAssets.Length > 0)
                    {
                        assetSize = builtAssets[0].packedSize.ToString();
                    }
                    else if (!s_ShaderVariantData.ContainsKey(shader))
                    {
                        // if not processed, it was not built into either player data or AssetBundles.
                        assetSize = "0";
                    }
                }
#endif
                onIncomingIssues(ProcessShader(shader, assetPath, assetSize, alwaysIncludedShaders.Contains(shader)));
                onIncomingIssues(ProcessVariants(platform, shader, assetPath));
            }
        }

        void ProcessComputeShaders(BuildTarget platform, Action<IEnumerable<ProjectIssue>> onIncomingIssues)
        {
#if COMPUTE_SHADER_ANALYSIS
            var issues = new List<ProjectIssue>();

            foreach (var shaderCompilerData in s_ComputeShaderVariantData)
            {
                var computeShaderName = shaderCompilerData.Key.name;
                foreach (var shaderVariantData in shaderCompilerData.Value)
                {
#if UNITY_2020_3_OR_NEWER
                    if (shaderVariantData.buildTarget != platform)
                        continue;
#endif

                    issues.Add(ProjectIssue.Create(k_ComputeShaderVariantLayout.category, computeShaderName)
                        .WithCustomProperties(new object[(int)ComputeShaderVariantProperty.Num]
                        {
                            shaderVariantData.compilerPlatform,
                            shaderVariantData.graphicsTier,
                            shaderVariantData.kernelName,
                            CombineKeywords(shaderVariantData.keywords),
                            CombineKeywords(shaderVariantData.platformKeywords)
                        }));
                }
            }
            if (issues.Any())
                onIncomingIssues(issues);
#endif
        }

        IEnumerable<ProjectIssue> ProcessShader(Shader shader, string assetPath, string assetSize, bool isAlwaysIncluded)
        {
            // set initial state (-1: info not available)
            var variantCountPerCompilerPlatform = s_ShaderVariantData.Count > 0 ? 0 : -1;

            // add variants first
            if (s_ShaderVariantData.ContainsKey(shader))
            {
                var variants = s_ShaderVariantData[shader];
                var numCompilerPlatforms = variants.Select(v => v.compilerPlatform).Distinct().Count();
                variantCountPerCompilerPlatform = variants.Count(v => ShaderTypeIsFragment(v.shaderType, v.compilerPlatform)) / numCompilerPlatforms;
            }

            var shaderName = shader.name;
            var shaderHasError = false;
            var severity = Severity.None;
#if UNITY_2019_1_OR_NEWER
            var shaderMessages = ShaderUtil.GetShaderMessages(shader);
            foreach (var shaderMessage in shaderMessages)
            {
                var message = shaderMessage.message;
                if (message.EndsWith("\n"))
                    message = message.Substring(0, message.Length - 2);
                yield return ProjectIssue.Create(IssueCategory.ShaderCompilerMessage, message)
                    .WithCustomProperties(new object[(int)ShaderMessageProperty.Num]
                    {
                        shaderName,
                        shaderMessage.platform
                    })
                    .WithLocation(assetPath, shaderMessage.line)
                    .WithSeverity(shaderMessage.severity == ShaderCompilerMessageSeverity.Error
                        ? Severity.Error
                        : Severity.Warning);
            }

            shaderHasError = ShaderUtil.ShaderHasError(shader);

            if (shaderHasError)
                severity = Severity.Error;
            else if (shaderMessages.Length > 0)
                severity = Severity.Warning;
#endif

            if (shaderHasError)
            {
                yield return ProjectIssue.Create(IssueCategory.Shader, Path.GetFileNameWithoutExtension(assetPath))
                    .WithCustomProperties((int)ShaderProperty.Num, k_NotAvailable)
                    .WithLocation(assetPath)
                    .WithSeverity(severity);
            }
            else
            {
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
                yield return ProjectIssue.Create(IssueCategory.Shader, shaderName)
                    .WithCustomProperties(new object[(int)ShaderProperty.Num]
                    {
                        assetSize,
                        ShaderUtilProxy.GetVariantCount(shader),
                        variantCountPerCompilerPlatform == -1 ? k_NotAvailable : variantCountPerCompilerPlatform.ToString(),
                        passCount == -1 ? k_NotAvailable : passCount.ToString(),
                        globalKeywords == null || localKeywords == null ? k_NotAvailable : (globalKeywords.Length + localKeywords.Length).ToString(),
                        shader.renderQueue,
                        hasInstancing,
                        isSrpBatcherCompatible,
                        isAlwaysIncluded
                    })
                    .WithLocation(assetPath)
                    .WithSeverity(severity);
            }
        }

        IEnumerable<ProjectIssue> ProcessVariants(BuildTarget platform, Shader shader, string assetPath)
        {
            if (s_ShaderVariantData.ContainsKey(shader))
            {
                var shaderVariants = s_ShaderVariantData[shader];

                foreach (var shaderVariantData in shaderVariants)
                {
#if UNITY_2020_3_OR_NEWER
                    if (shaderVariantData.buildTarget != platform)
                        continue;
#endif

                    yield return ProjectIssue.Create(IssueCategory.ShaderVariant, shader.name)
                        .WithLocation(assetPath)
                        .WithCustomProperties(new object[(int)ShaderVariantProperty.Num]
                        {
                            k_NoRuntimeData,
                            shaderVariantData.compilerPlatform,
                            shaderVariantData.graphicsTier,
                            shaderVariantData.shaderType,
                            shaderVariantData.passType,
                            shaderVariantData.passName,
                            CombineKeywords(shaderVariantData.keywords),
                            CombineKeywords(shaderVariantData.platformKeywords),
                            CombineKeywords(shaderVariantData.requirements.Select(r => r.ToString()).ToArray())
                        });
                }
            }
        }

        internal static void ClearBuildData()
        {
            s_ShaderVariantData.Clear();
#if COMPUTE_SHADER_ANALYSIS
            s_ComputeShaderVariantData.Clear();
#endif

#if UNITY_2021_1_OR_NEWER
            var playerDataCachePath = Path.Combine("Library", "PlayerDataCache");
            if (Directory.Exists(playerDataCachePath))
            {
                Directory.Delete(playerDataCachePath, true);
            }
#endif
        }

        internal static int NumBuiltVariants()
        {
            return s_ShaderVariantData.Count;
        }

        public int callbackOrder => Int32.MaxValue;

#if COMPUTE_SHADER_ANALYSIS
        public void OnProcessComputeShader(ComputeShader shader, string kernelName, IList<ShaderCompilerData> data)
        {
            if (!s_ComputeShaderVariantData.ContainsKey(shader))
            {
                s_ComputeShaderVariantData.Add(shader, new List<ComputeShaderVariantData>());
            }

            foreach (var shaderCompilerData in data)
            {
                s_ComputeShaderVariantData[shader].Add(new ComputeShaderVariantData
                {
                    kernelName = kernelName,
                    keywords = GetShaderKeywords(shader, shaderCompilerData.shaderKeywordSet.GetShaderKeywords()),
                    platformKeywords = PlatformKeywordSetToStrings(shaderCompilerData.platformKeywordSet),
                    graphicsTier = shaderCompilerData.graphicsTier,
#if UNITY_2020_3_OR_NEWER
                    buildTarget = shaderCompilerData.buildTarget,
#endif
                    compilerPlatform = shaderCompilerData.shaderCompilerPlatform
                });
            }
        }

#endif

        public void OnProcessShader(Shader shader, ShaderSnippetData snippet, IList<ShaderCompilerData> data)
        {
            if (data.Count == 0)
                return; // no variants

            if (!s_ShaderVariantData.ContainsKey(shader))
            {
                s_ShaderVariantData.Add(shader, new List<ShaderVariantData>());
            }

            foreach (var shaderCompilerData in data)
            {
                var shaderRequirements = shaderCompilerData.shaderRequirements;
                var shaderRequirementsList = new List<ShaderRequirements>();
                foreach (ShaderRequirements value in Enum.GetValues(shaderRequirements.GetType()))
                    if ((shaderRequirements & value) != 0)
                        shaderRequirementsList.Add(value);

                if (shaderRequirementsList.Count > 1)
                    shaderRequirementsList.Remove(ShaderRequirements.None);

                s_ShaderVariantData[shader].Add(new ShaderVariantData
                {
                    passType = snippet.passType,
                    passName =  snippet.passName,
                    shaderType = snippet.shaderType,
                    keywords = GetShaderKeywords(shader, shaderCompilerData.shaderKeywordSet.GetShaderKeywords()),
                    platformKeywords = PlatformKeywordSetToStrings(shaderCompilerData.platformKeywordSet),
                    requirements = shaderRequirementsList.ToArray(),
                    graphicsTier = shaderCompilerData.graphicsTier,
#if UNITY_2020_3_OR_NEWER
                    buildTarget = shaderCompilerData.buildTarget,
#endif
                    compilerPlatform = shaderCompilerData.shaderCompilerPlatform
                });
            }
        }

        public static void ExportSVC(string svcName, string path, ProjectIssue[] variants)
        {
            var svc = new ShaderVariantCollection();
            svc.name = svcName;

            foreach (var issue in variants)
            {
                var shader = Shader.Find(issue.GetProperty(PropertyType.Description));
                var passType = issue.GetCustomProperty(ShaderVariantProperty.PassType);
                var keywords = SplitKeywords(issue.GetCustomProperty(ShaderVariantProperty.Keywords));

                if (shader != null && !passType.Equals(string.Empty))
                {
                    var shaderVariant = new ShaderVariantCollection.ShaderVariant();
                    shaderVariant.shader = shader;
                    shaderVariant.passType = (UnityEngine.Rendering.PassType)Enum.Parse(typeof(UnityEngine.Rendering.PassType), passType);
                    shaderVariant.keywords = keywords;
                    svc.Add(shaderVariant);
                }
            }
            AssetDatabase.CreateAsset(svc, path);
        }

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
                var keywords = SplitKeywords(keywordsString, " ");

                // fix-up stage to be consistent with built variants stage
                if (k_StageNameMap.ContainsKey(stage))
                    stage = k_StageNameMap[stage];

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
                var stage = builtVariant.GetCustomProperty(ShaderVariantProperty.Stage);
                var passName = builtVariant.GetCustomProperty(ShaderVariantProperty.PassName);
                var keywordsString = builtVariant.GetCustomProperty(ShaderVariantProperty.Keywords);
                var keywords = SplitKeywords(keywordsString);
                var isVariantCompiled = false;

                if (compiledVariants.ContainsKey(shaderName))
                {
                    // note that we are not checking pass name since there is an inconsistency regarding "unnamed" passes between build vs compiled
                    var matchingVariants = compiledVariants[shaderName].Where(cv => ShaderVariantsMatch(cv, stage, passName, keywords)).ToArray();
                    isVariantCompiled = matchingVariants.Length > 0;
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
                        var compilationLogIndex = line.IndexOf(prefix, StringComparison.Ordinal);
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

        static bool ShaderVariantsMatch(CompiledVariantData cv, string stage, string passName, string[] secondSet)
        {
            if (!cv.stage.Equals(stage, StringComparison.InvariantCultureIgnoreCase))
                return false;

            var passMatch = cv.pass.Equals(passName);
            if (!passMatch)
            {
                var isUnnamed = k_NoPassNames.Contains(cv.pass) || cv.pass.StartsWith("<Unnamed Pass ");
#if UNITY_2021_3_OR_NEWER || UNITY_2021_2_14 || UNITY_2021_2_15 || UNITY_2021_2_16 || UNITY_2021_2_17 || UNITY_2021_2_18 || UNITY_2021_2_19
                passMatch = isUnnamed && string.IsNullOrEmpty(passName);
#elif UNITY_2019_1_OR_NEWER
                var pass = 0;
                passMatch = isUnnamed && passName.StartsWith(k_UnnamedPassPrefix) && int.TryParse(passName.Substring(k_UnnamedPassPrefix.Length), out pass);
#else
                passMatch = isUnnamed && string.IsNullOrEmpty(passName);
#endif
            }

            if (!passMatch)
                return false;
            return cv.keywords.OrderBy(e => e).SequenceEqual(secondSet.OrderBy(e => e));
        }

        static string[] GetShaderKeywords(Shader shader, ShaderKeyword[] shaderKeywords)
        {
#if UNITY_2021_2_OR_NEWER
            var keywords = shaderKeywords.Select(keyword => keyword.name);
#elif UNITY_2019_3_OR_NEWER
            var keywords = shaderKeywords.Select(keyword => ShaderKeyword.IsKeywordLocal(keyword) ? ShaderKeyword.GetKeywordName(shader, keyword) : ShaderKeyword.GetGlobalKeywordName(keyword));
#else
            var keywords = shaderKeywords.Select(keyword => keyword.GetKeywordName());
#endif
            return keywords.ToArray();
        }

#if COMPUTE_SHADER_ANALYSIS
        static string[] GetShaderKeywords(ComputeShader shader, ShaderKeyword[] shaderKeywords)
        {
#if UNITY_2021_2_OR_NEWER
            var keywords = shaderKeywords.Select(keyword => keyword.name);
#elif UNITY_2019_3_OR_NEWER
            var keywords = shaderKeywords.Select(keyword => ShaderKeyword.IsKeywordLocal(keyword) ? ShaderKeyword.GetKeywordName(shader, keyword) : ShaderKeyword.GetGlobalKeywordName(keyword));
#else
            var keywords = shaderKeywords.Select(keyword => keyword.GetKeywordName());
#endif
            return keywords.ToArray();
        }

#endif
        static string[] SplitKeywords(string keywordsString, string separator = null)
        {
            if (keywordsString.Equals(k_NoKeywords))
                return new string[] {};
            return Formatting.SplitStrings(keywordsString, separator);
        }

        static string CombineKeywords(string[] strings, string separator = null)
        {
            if (strings.Length > 0)
                return Formatting.CombineStrings(strings, separator);
            return k_NoKeywords;
        }

        static string[] PlatformKeywordSetToStrings(PlatformKeywordSet platformKeywordSet)
        {
            var builtinShaderDefines = new List<BuiltinShaderDefine>();

            foreach (BuiltinShaderDefine value in Enum.GetValues(typeof(BuiltinShaderDefine)))
                if (platformKeywordSet.IsEnabled(value))
                    builtinShaderDefines.Add(value);

            return builtinShaderDefines.Select(d => d.ToString()).ToArray();
        }

        static bool ShaderTypeIsFragment(ShaderType shaderType, ShaderCompilerPlatform shaderCompilerPlatform)
        {
            switch (shaderCompilerPlatform)
            {
                // On OpenGL and Vulkan, all stages supported by the shader are combined into a single ShaderType (Vertex).
                case ShaderCompilerPlatform.GLES20:
                case ShaderCompilerPlatform.GLES3x:
                case ShaderCompilerPlatform.OpenGLCore:
                case ShaderCompilerPlatform.Vulkan:
                    return true;
                default:
                    return shaderType == ShaderType.Fragment;
            }
        }
    }
}
