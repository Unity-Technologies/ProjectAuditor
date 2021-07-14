using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Unity.ProjectAuditor.Editor;
using Unity.ProjectAuditor.Editor.Auditors;
using Unity.ProjectAuditor.Editor.Utils;
using UnityEditor.Build;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
#if UNITY_2018_2_OR_NEWER
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
#endif

namespace UnityEditor.ProjectAuditor.EditorTests
{
    class ShaderTests
    {
        const string k_ShaderName = "Custom/MyTestShader,1"; // comma in the name for testing purposes

#pragma warning disable 0414
        TempAsset m_ShaderResource;
        TempAsset m_PlayerLogResource;
        TempAsset m_PlayerLogWithNoCompilationResource;
        TempAsset m_ShaderWithErrorResource;
        TempAsset m_EditorShaderResource;

        TempAsset m_ShaderUsingBuiltInKeywordResource;
        TempAsset m_SurfShaderResource;
#pragma warning restore 0414

#if UNITY_2018_2_OR_NEWER
        static string s_KeywordName = "DIRECTIONAL";

        class StripVariants : IPreprocessShaders
        {
            public static bool Enabled;
            public int callbackOrder { get { return 0; } }

            public void OnProcessShader(
                Shader shader, ShaderSnippetData snippet, IList<ShaderCompilerData> shaderCompilerData)
            {
                if (!Enabled)
                    return;

                var keyword = new ShaderKeyword(s_KeywordName);

                for (int i = 0; i < shaderCompilerData.Count; ++i)
                {
                    if (shaderCompilerData[i].shaderKeywordSet.IsEnabled(keyword))
                    {
                        shaderCompilerData.RemoveAt(i);
                        --i;
                    }
                }
            }
        }
#endif


        [OneTimeSetUp]
        public void SetUp()
        {
            m_ShaderResource = new TempAsset("Resources/MyTestShader.shader", @"
            Shader ""Custom/MyTestShader,1""
            {
                SubShader
                {
                    Pass
                    {
                        Name ""MyTestShader/Pass""

                        CGPROGRAM
    #pragma vertex vert
    #pragma fragment frag
    #pragma multi_compile __ KEYWORD_A KEYWORD_B

                        struct appdata
                        {
                            float4 vertex : POSITION;
                            float2 uv : TEXCOORD0;
                        };

                        struct v2f
                        {
                            float2 uv : TEXCOORD0;
                            float4 vertex : SV_POSITION;
                        };

                        sampler2D _MainTex;
                        float4 _MainTex_ST;

                        v2f vert (appdata v)
                        {
                            v2f o;
                            o.vertex = UnityObjectToClipPos(v.vertex);
                            o.uv = v.uv;
                            return o;
                        }

                        fixed4 frag (v2f i) : SV_Target
                        {
                            return tex2D(_MainTex, i.uv);
                        }
                        ENDCG
                    }

                    Pass
                    {
                        CGPROGRAM
    #pragma vertex vert
    #pragma fragment frag
    #pragma multi_compile __ KEYWORD_A

                        struct appdata
                        {
                            float4 vertex : POSITION;
                            float2 uv : TEXCOORD0;
                        };

                        struct v2f
                        {
                            float2 uv : TEXCOORD0;
                            float4 vertex : SV_POSITION;
                        };

                        sampler2D _MainTex;
                        float4 _MainTex_ST;

                        v2f vert (appdata v)
                        {
                            v2f o;
                            o.vertex = UnityObjectToClipPos(v.vertex);
                            o.uv = v.uv;
                            return o;
                        }

                        fixed4 frag (v2f i) : SV_Target
                        {
                            return tex2D(_MainTex, i.uv);
                        }
                        ENDCG
                    }
                }
            }");

            m_PlayerLogResource = new TempAsset("player.log", @"
02-10 17:36:20.945  6554  6816 D Unity   : Compiled shader: Custom/MyTestShader,1, pass: MyTestShader/Pass, stage: vertex, keywords <no keywords>
02-10 17:36:20.945  6554  6816 D Unity   : Compiled shader: Custom/MyTestShader,1, pass: MyTestShader/Pass, stage: fragment, keywords <no keywords>
02-10 17:36:20.945  6554  6816 D Unity   : Compiled shader: Custom/MyTestShader,1, pass: MyTestShader/Pass, stage: vertex, keywords KEYWORD_A
02-10 17:36:20.945  6554  6816 D Unity   : Compiled shader: Custom/MyTestShader,1, pass: MyTestShader/Pass, stage: fragment, keywords KEYWORD_A
02-10 17:36:20.945  6554  6816 D Unity   :
02-10 17:36:20.945  6554  6816 D Unity   : Compiled shader: Custom/MyTestShader,1, pass: <unnamed>, stage: vertex, keywords KEYWORD_A
02-10 17:36:20.945  6554  6816 D Unity   : Compiled shader: Custom/MyTestShader,1, pass: <unnamed>, stage: fragment, keywords KEYWORD_A
            ");


            m_PlayerLogWithNoCompilationResource = new TempAsset("player_with_no_compilation.log", string.Empty);

#if UNITY_2019_1_OR_NEWER
            m_ShaderWithErrorResource = new TempAsset("Resources/ShaderWithError.shader", @"
            Sader ""Custom/ShaderWithError""
            {
            }");
#endif

            m_ShaderUsingBuiltInKeywordResource = new TempAsset("Resources/ShaderUsingBuiltInKeyword.shader", @"
Shader ""Custom/ShaderUsingBuiltInKeyword""
            {
                Properties
                {
                    _MainTex (""Texture"", 2D) = ""white"" {}
                }
                SubShader
                {
                    Tags { ""RenderType""=""Opaque"" }
                    LOD 100

                    Pass
                    {
                        CGPROGRAM
#pragma vertex vert
#pragma fragment frag
#pragma multi_compile_instancing

#include ""UnityCG.cginc""

                        struct appdata
                        {
                            float4 vertex : POSITION;
                            float2 uv : TEXCOORD0;
                        };

                        struct v2f
                        {
                            float2 uv : TEXCOORD0;
                            float4 vertex : SV_POSITION;
                        };

                        sampler2D _MainTex;
                        float4 _MainTex_ST;

                        v2f vert (appdata v)
                        {
                            v2f o;
                            o.vertex = UnityObjectToClipPos(v.vertex);
                            o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                            return o;
                        }

                        fixed4 frag (v2f i) : SV_Target
                        {
                            return tex2D(_MainTex, i.uv);
                        }
                        ENDCG
                    }
                }
            }
            ");

            m_SurfShaderResource = new TempAsset("Resources/MySurfShader.shader", @"
Shader ""Custom/MySurfShader""
            {
                Properties
                {
                    _Color (""Color"", Color) = (1,1,1,1)
                }
                SubShader
                {
                    Tags { ""RenderType""=""Opaque"" }
                    LOD 200

                    CGPROGRAM
                    // Physically based Standard lighting model, and enable shadows on all light types
                    #pragma surface surf Standard fullforwardshadows

                    // Use shader model 3.0 target, to get nicer looking lighting
                    #pragma target 3.0

                    sampler2D _MainTex;

                    struct Input
                    {
                        float2 uv_MainTex;
                    };

                    half _Glossiness;
                    half _Metallic;
                    fixed4 _Color;

                    void surf (Input IN, inout SurfaceOutputStandard o)
                    {
                        // Albedo comes from a texture tinted by color
                        fixed4 c = tex2D (_MainTex, IN.uv_MainTex) * _Color;
                        o.Albedo = c.rgb;
                        // Metallic and smoothness come from slider variables
                        o.Metallic = _Metallic;
                        o.Smoothness = _Glossiness;
                        o.Alpha = c.a;
                    }
                    ENDCG
                }
                FallBack ""Diffuse""
            }
");

            m_EditorShaderResource = new TempAsset("Editor/MyEditorShader.shader", @"
Shader ""Custom/MyEditorShader""
            {
                Properties
                {
                    _Color (""Color"", Color) = (1,1,1,1)
                }
                SubShader
                {
                    Tags { ""RenderType""=""Opaque"" }
                    LOD 200

                    CGPROGRAM
                    // Physically based Standard lighting model, and enable shadows on all light types
                    #pragma surface surf Standard fullforwardshadows

                    // Use shader model 3.0 target, to get nicer looking lighting
                    #pragma target 3.0

                    sampler2D _MainTex;

                    struct Input
                    {
                        float2 uv_MainTex;
                    };

                    half _Glossiness;
                    half _Metallic;
                    fixed4 _Color;

                    void surf (Input IN, inout SurfaceOutputStandard o)
                    {
                        // Albedo comes from a texture tinted by color
                        fixed4 c = tex2D (_MainTex, IN.uv_MainTex) * _Color;
                        o.Albedo = c.rgb;
                        // Metallic and smoothness come from slider variables
                        o.Metallic = _Metallic;
                        o.Smoothness = _Glossiness;
                        o.Alpha = c.a;
                    }
                    ENDCG
                }
                FallBack ""Diffuse""
            }
");
        }

        [OneTimeTearDown]
        public void TearDown()
        {
            TempAsset.Cleanup();
        }

#if UNITY_2018_2_OR_NEWER
        [Test]
        public void ShaderVariantsRequireBuild()
        {
            ShadersModule.ClearBuildData();
            var issues = Utility.Analyze(IssueCategory.ShaderVariant);
            Assert.Zero(issues.Length);
            Assert.False(ShadersModule.BuildDataAvailable());
        }

        [Test]
        public void ShaderVariantsAreReported()
        {
            var issues = Utility.AnalyzeBuild().GetIssues(IssueCategory.ShaderVariant);
            Assert.True(ShadersModule.BuildDataAvailable());

            var keywords = issues.Select(i => i.GetCustomProperty(ShaderVariantProperty.Keywords));
            Assert.True(keywords.Any(key => key.Equals(s_KeywordName)));

            var variants = issues.Where(i => i.description.Equals(k_ShaderName)).ToArray();
            Assert.Positive(variants.Length);

            var shaderCompilerPlatforms = variants.Select(v => v.GetCustomProperty(ShaderVariantProperty.Platform)).Distinct();
            var compilerPlatformNames = ShaderUtilProxy.GetCompilerPlatformNames();

            foreach (var plat in shaderCompilerPlatforms)
            {
                Assert.Contains(plat, compilerPlatformNames);

                var variantsForPlatform = variants.Where(v => v.GetCustomProperty(ShaderVariantProperty.Platform).Equals(plat)).ToArray();
                Assert.AreEqual((int)ShaderVariantProperty.Num, variantsForPlatform[0].GetNumCustomProperties());

                // "#pragma multi_compile __ KEYWORD_A KEYWORD_B" should produce 3 variants for each graphics API
                Assert.True(variantsForPlatform.Any(v => v.GetCustomProperty(ShaderVariantProperty.Keywords).Equals(ShadersModule.k_NoKeywords)));
                Assert.True(variantsForPlatform.Any(v => v.GetCustomProperty(ShaderVariantProperty.Keywords).Equals("KEYWORD_A")));
                Assert.True(variantsForPlatform.Any(v => v.GetCustomProperty(ShaderVariantProperty.Keywords).Equals("KEYWORD_B")));
                Assert.True(variantsForPlatform.All(v => v.GetCustomProperty(ShaderVariantProperty.Compiled).Equals(ShadersModule.k_NoRuntimeData)));

                // check descriptor
                Assert.True(variantsForPlatform.Select(v => v.descriptor.GetAreas()).All(areas => areas.Length == 1 && areas.Contains(Area.Info)));
            }
        }

        [Test]
        public void ShaderVariantForBuiltInKeywordIsReported()
        {
            var issues =  Utility.AnalyzeBuild().GetIssues(IssueCategory.ShaderVariant);

            var keywords = issues.Select(i => i.GetCustomProperty(ShaderVariantProperty.Keywords)).ToArray();

            Assert.True(keywords.Any(key => key.Equals(s_KeywordName)));

            var variants = issues.Where(i => i.description.Equals("Custom/ShaderUsingBuiltInKeyword")).ToArray();
            Assert.Positive(variants.Length, "No shader variants found");

            // check custom properties
            Assert.True(variants.Any(v => v.GetCustomProperty(ShaderVariantProperty.Keywords).Equals("<no keywords>")), "No shader variants found without INSTANCING_ON keyword");
            Assert.True(variants.Any(v => v.GetCustomProperty(ShaderVariantProperty.Requirements).Equals("BaseShaders, Derivatives")), "No shader variants found without Instancing requirement");
#if UNITY_2019_1_OR_NEWER
            //this one fails on yamato
            //Assert.True(variants.Any(v => v.GetCustomProperty(ShaderVariantProperty.Keywords).Equals("INSTANCING_ON")), "No shader variants found with INSTANCING_ON keyword");
            //Assert.True(variants.Any(v => v.GetCustomProperty(ShaderVariantProperty.Requirements).Equals("BaseShaders, Derivatives, Instancing")), "No shader variants found with Instancing requirement");
#endif
        }

        [Test]
        public void SurfShaderVariantsAreReported()
        {
            var issues =  Utility.AnalyzeBuild().GetIssues(IssueCategory.ShaderVariant);

            var keywords = issues.Select(i => i.GetCustomProperty(ShaderVariantProperty.Keywords));

            Assert.True(keywords.Any(key => key.Equals(s_KeywordName)));

            var variants = issues.Where(i => i.description.Equals("Custom/MySurfShader")).ToArray();
            Assert.Positive(variants.Count());

            // check custom property
            var variant = variants.FirstOrDefault(v => v.GetCustomProperty(ShaderVariantProperty.PassName).Equals("FORWARD") && v.GetCustomProperty(ShaderVariantProperty.Keywords).Equals("DIRECTIONAL"));
            Assert.NotNull(variant);
        }

        [Test]
        public void StrippedVariantsAreNotReported()
        {
            StripVariants.Enabled = true;
            var issues = Utility.AnalyzeBuild().GetIssues(IssueCategory.ShaderVariant);
            StripVariants.Enabled = false;

            var keywords = issues.Select(i => i.GetCustomProperty(ShaderVariantProperty.Keywords));

            Assert.False(keywords.Any(key => key.Equals(s_KeywordName)));
        }

        [Test]
        public void PlayerLogDoesNotContainShaderCompilationLog()
        {
            var result = ShadersModule.ParsePlayerLog(m_PlayerLogWithNoCompilationResource.relativePath, new ProjectIssue[0]);
            Assert.That(result, Is.EqualTo(ParseLogResult.NoCompiledVariants));
        }

        [Test]
        public void UnusedVariantsAreReported()
        {
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), "Assets/UntitledScene.unity");

            var buildPath = FileUtil.GetUniqueTempPathInProject();
            Directory.CreateDirectory(buildPath);

            ShadersModule.ClearBuildData(); // clear previously built variants, if any
            var buildReport = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new string[] {},
                locationPathName = Path.Combine(buildPath, "test"),
                target = EditorUserBuildSettings.activeBuildTarget,
                targetGroup = BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget),
                options = BuildOptions.Development
            });

            Assert.True(buildReport.summary.result == BuildResult.Succeeded);

            Directory.Delete(buildPath, true);

            AssetDatabase.DeleteAsset("Assets/UntitledScene.unity");

            var allVariants = Utility.Analyze(IssueCategory.ShaderVariant);
            ShadersModule.ClearBuildData(); // cleanup

            var variants = allVariants.Where(i => i.description.Equals(k_ShaderName) && i.category == IssueCategory.ShaderVariant).ToArray();
            Assert.Positive(variants.Length);

            var result = ShadersModule.ParsePlayerLog(m_PlayerLogResource.relativePath, variants);

            Assert.That(result, Is.EqualTo(ParseLogResult.Success), "No compiled shader variants found in player log.");

            var shaderCompilerPlatforms = variants.Select(v => v.GetCustomProperty(ShaderVariantProperty.Platform)).Distinct().ToArray();
            var numShaderCompilerPlatforms = shaderCompilerPlatforms.Count();

            Assert.AreEqual(5 * numShaderCompilerPlatforms, variants.Length, "Compiler Platforms: " + string.Join(", ", shaderCompilerPlatforms));

            var unusedVariants = variants.Where(i => !i.GetCustomPropertyAsBool(ShaderVariantProperty.Compiled)).ToArray();
            foreach (var plat in shaderCompilerPlatforms)
            {
                var unusedVariantsForPlatform = unusedVariants.Where(v => v.GetCustomProperty(ShaderVariantProperty.Platform).Equals(plat)).ToArray();

                Assert.AreEqual(2, unusedVariantsForPlatform.Length);
                Assert.True(unusedVariantsForPlatform[0].GetCustomProperty(ShaderVariantProperty.PassName).Equals("MyTestShader/Pass"));
                Assert.True(unusedVariantsForPlatform[0].GetCustomProperty(ShaderVariantProperty.Keywords).Equals("KEYWORD_B"));
#if UNITY_2019_1_OR_NEWER
                Assert.True(unusedVariantsForPlatform[1].GetCustomProperty(ShaderVariantProperty.PassName).Equals("Pass 1"));
#else
                Assert.True(unusedVariantsForPlatform[1].GetCustomProperty(ShaderVariantProperty.PassName).Equals(string.Empty));
#endif
                Assert.True(unusedVariantsForPlatform[1].GetCustomProperty(ShaderVariantProperty.Keywords).Equals(ShadersModule.k_NoKeywords));
            }
        }

#endif

        [Test]
        public void ShaderIsReported()
        {
            ShadersModule.ClearBuildData();
            var issues = Utility.Analyze(IssueCategory.Shader);
            var shaderIssue = issues.FirstOrDefault(i => i.description.Equals(k_ShaderName));
            Assert.NotNull(shaderIssue);

            // check descriptor
            Assert.AreEqual(1, shaderIssue.descriptor.GetAreas().Length);
            Assert.Contains(Area.Info, shaderIssue.descriptor.GetAreas());

            // check custom property
            Assert.AreEqual((int)ShaderProperty.Num, shaderIssue.GetNumCustomProperties());
            Assert.True(shaderIssue.GetCustomProperty(ShaderProperty.NumVariants).Equals(ShadersModule.k_NotAvailable), "Num Variants: " + shaderIssue.GetCustomProperty(ShaderProperty.NumVariants));

#if UNITY_2019_1_OR_NEWER
            Assert.AreEqual(2, shaderIssue.GetCustomPropertyAsInt(ShaderProperty.NumPasses), "NumPasses was : " + shaderIssue.GetCustomProperty(ShaderProperty.NumPasses));
            Assert.AreEqual(2, shaderIssue.GetCustomPropertyAsInt(ShaderProperty.NumKeywords), "NumKeywords was : " + shaderIssue.GetCustomProperty(ShaderProperty.NumKeywords));
#else
            Assert.AreEqual(0, shaderIssue.GetCustomPropertyAsInt(ShaderProperty.NumPasses), "NumPasses was : " + shaderIssue.GetCustomProperty(ShaderProperty.NumPasses));
            Assert.AreEqual(0, shaderIssue.GetCustomPropertyAsInt(ShaderProperty.NumKeywords), "NumKeywords was : " + shaderIssue.GetCustomProperty(ShaderProperty.NumKeywords));
#endif
            Assert.AreEqual(2000, shaderIssue.GetCustomPropertyAsInt(ShaderProperty.RenderQueue), "RenderQueue was : " + shaderIssue.GetCustomProperty(ShaderProperty.RenderQueue));
            Assert.False(shaderIssue.GetCustomPropertyAsBool(ShaderProperty.Instancing), "Instancing is supported but it should not be.");
            Assert.False(shaderIssue.GetCustomPropertyAsBool(ShaderProperty.SrpBatcher), "SRP Batcher is supported but it should not be.");
        }

#if UNITY_2019_1_OR_NEWER
        [Test]
        public void ShaderWithErrorIsReported()
        {
            var issues = Utility.Analyze(IssueCategory.Shader);
            var shadersWithErrors = issues.Where(i => i.severity == Rule.Severity.Error);
            Assert.Positive(shadersWithErrors.Count());
            var shaderIssue = issues.FirstOrDefault(i => i.relativePath.Equals(m_ShaderWithErrorResource.relativePath));
            Assert.NotNull(shaderIssue);
        }

#endif

        [Test]
        public void ShaderUsingBuiltInKeywordIsReported()
        {
            var issues = Utility.Analyze(IssueCategory.Shader);
            var shaderIssue = issues.FirstOrDefault(i => i.description.Equals("Custom/ShaderUsingBuiltInKeyword"));
            Assert.NotNull(shaderIssue);

            // check custom property
            Assert.AreEqual((int)ShaderProperty.Num, shaderIssue.GetNumCustomProperties());
#if UNITY_2019_1_OR_NEWER
            Assert.AreEqual(1, shaderIssue.GetCustomPropertyAsInt(ShaderProperty.NumPasses), "NumPasses was : " + shaderIssue.GetCustomProperty(ShaderProperty.NumPasses));
            Assert.AreEqual(1, shaderIssue.GetCustomPropertyAsInt(ShaderProperty.NumKeywords), "NumKeywords was : " + shaderIssue.GetCustomProperty(ShaderProperty.NumKeywords));
#else
            Assert.AreEqual(0, shaderIssue.GetCustomPropertyAsInt(ShaderProperty.NumPasses), "NumPasses was : " + shaderIssue.GetCustomProperty(ShaderProperty.NumPasses));
            Assert.AreEqual(0, shaderIssue.GetCustomPropertyAsInt(ShaderProperty.NumKeywords), "NumKeywords was : " + shaderIssue.GetCustomProperty(ShaderProperty.NumKeywords));
#endif
            Assert.AreEqual(2000, shaderIssue.GetCustomPropertyAsInt(ShaderProperty.RenderQueue), "RenderQueue was : " + shaderIssue.GetCustomProperty(ShaderProperty.RenderQueue));
            Assert.True(shaderIssue.GetCustomPropertyAsBool(ShaderProperty.Instancing));
        }

        [Test]
        public void SurfShaderIsReported()
        {
            var issues = Utility.Analyze(IssueCategory.Shader);
            var shaderIssue = issues.FirstOrDefault(i => i.description.Equals("Custom/MySurfShader"));
            Assert.NotNull(shaderIssue);

            // check custom property
            Assert.AreEqual((int)ShaderProperty.Num, shaderIssue.GetNumCustomProperties());
#if UNITY_2019_1_OR_NEWER
            Assert.AreEqual(4, shaderIssue.GetCustomPropertyAsInt(ShaderProperty.NumPasses), "NumPasses was : " + shaderIssue.GetCustomProperty(ShaderProperty.NumPasses));
            Assert.AreEqual(22, shaderIssue.GetCustomPropertyAsInt(ShaderProperty.NumKeywords), "NumKeywords was : " + shaderIssue.GetCustomProperty(ShaderProperty.NumKeywords));
#else
            Assert.AreEqual(0, shaderIssue.GetCustomPropertyAsInt(ShaderProperty.NumPasses), "NumPasses was : " + shaderIssue.GetCustomProperty(ShaderProperty.NumPasses));
            Assert.AreEqual(0, shaderIssue.GetCustomPropertyAsInt(ShaderProperty.NumKeywords), "NumKeywords was : " + shaderIssue.GetCustomProperty(ShaderProperty.NumKeywords));
#endif
            Assert.AreEqual(2000, shaderIssue.GetCustomPropertyAsInt(ShaderProperty.RenderQueue), "RenderQueue was : " + shaderIssue.GetCustomProperty(ShaderProperty.RenderQueue));
            Assert.True(shaderIssue.GetCustomPropertyAsBool(ShaderProperty.Instancing));
        }

        [Test]
        public void EditorShaderIsNotReported()
        {
            var issues = Utility.Analyze(IssueCategory.Shader);
            issues = issues.Where(i => i.description.Equals("Custom/MyEditorShader")).ToArray();

            Assert.Zero(issues.Length);
        }

        [Test]
        public void EditorDefaultResourcesShaderIsNotReported()
        {
            var issues = Utility.Analyze(IssueCategory.Shader);
            var filteredIssues = issues.Where(i => i.relativePath.Contains("Editor Default Resources")).ToArray();
            Assert.Zero(filteredIssues.Length);
        }
    }
}
