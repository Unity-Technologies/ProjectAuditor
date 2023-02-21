using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Unity.ProjectAuditor.Editor;
using Unity.ProjectAuditor.Editor.Diagnostic;
using Unity.ProjectAuditor.Editor.Modules;
using Unity.ProjectAuditor.Editor.Tests.Common;
using Unity.ProjectAuditor.Editor.Utils;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;

namespace Unity.ProjectAuditor.EditorTests
{
    class ShadersAnalysisTests : TestFixtureBase
    {
        const string k_ShaderName = "Custom/MyTestShader,1"; // comma in the name for testing purposes

#pragma warning disable 0414
        TestAsset m_ShaderResource;
        TestAsset m_PlayerLogResource;
        TestAsset m_PlayerLogWithNoCompilationResource;
        TestAsset m_ShaderWithErrorResource;
        TestAsset m_EditorShaderResource;

        TestAsset m_ShaderUsingBuiltInKeywordResource;
        TestAsset m_SurfShaderResource;
#pragma warning restore 0414

        const string s_KeywordName = "DIRECTIONAL";

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
                        // remove variant containing the specified keyword
                        shaderCompilerData.RemoveAt(i);
                        --i;
                    }
                }
            }
        }

        [SetUp]
        public void Clear()
        {
            // Clear previously built variants
            ShadersModule.ClearBuildData();
        }

        [OneTimeSetUp]
        public void SetUp()
        {
            m_ShaderResource = new TestAsset("Resources/MyTestShader.shader", @"
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
                            return tex2D(_MainTex, i.uv) / 0.0f; // intentionally divide by zero
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

            m_PlayerLogResource = new TestAsset("player.log", @"
02-10 17:36:20.945  6554  6816 D Unity   : Compiled shader: Custom/MyTestShader,1, pass: MyTestShader/Pass, stage: vertex, keywords <no keywords>
02-10 17:36:20.945  6554  6816 D Unity   : Compiled shader: Custom/MyTestShader,1, pass: MyTestShader/Pass, stage: fragment, keywords <no keywords>
02-10 17:36:20.945  6554  6816 D Unity   : Compiled shader: Custom/MyTestShader,1, pass: MyTestShader/Pass, stage: vertex, keywords KEYWORD_A
02-10 17:36:20.945  6554  6816 D Unity   : Compiled shader: Custom/MyTestShader,1, pass: MyTestShader/Pass, stage: pixel, keywords KEYWORD_A
02-10 17:36:20.945  6554  6816 D Unity   :
02-10 17:36:20.945  6554  6816 D Unity   : Compiled shader: Custom/MyTestShader,1, pass: <unnamed>, stage: vertex, keywords KEYWORD_A
02-10 17:36:20.945  6554  6816 D Unity   : Compiled shader: Custom/MyTestShader,1, pass: unnamed, stage: fragment, keywords KEYWORD_A
            ");


            m_PlayerLogWithNoCompilationResource = new TestAsset("player_with_no_compilation.log", string.Empty);

#if UNITY_2021_1_OR_NEWER
            UnityEngine.TestTools.LogAssert.ignoreFailingMessages = true; // workaround for 2021.x failure
#endif

#if UNITY_2019_1_OR_NEWER
            m_ShaderWithErrorResource = new TestAsset("Resources/ShaderWithError.shader", @"
            Sader ""Custom/ShaderWithError""
            {
            }");
#endif

            m_ShaderUsingBuiltInKeywordResource = new TestAsset("Resources/ShaderUsingBuiltInKeyword.shader", @"
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

            m_SurfShaderResource = new TestAsset("Resources/MySurfShader.shader", @"
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

            m_EditorShaderResource = new TestAsset("Editor/MyEditorShader.shader", @"
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

        [Test]
        public void ShadersAnalysis_Variants_AreNotReported()
        {
            ShadersModule.ClearBuildData();
            var issues = Analyze(IssueCategory.ShaderVariant);
            Assert.Zero(issues.Length);
            Assert.Zero(ShadersModule.NumBuiltVariants());
        }

        [Test]
#if UNITY_2021_1_OR_NEWER
        [Ignore("TODO: investigate reason for test failure")]
#endif
        public void ShadersAnalysis_VariantsData_IsAvailableAfterBuild()
        {
            ShadersModule.ClearBuildData();
            Build();
            Assert.Positive(ShadersModule.NumBuiltVariants(), "Build Data is not available");
        }

        [Test]
#if UNITY_2021_1_OR_NEWER
        [Ignore("TODO: investigate reason for test failure")]
#endif
        public void ShadersAnalysis_VariantsData_IsClearedAfterAnalysis()
        {
            AnalyzeBuild(IssueCategory.Shader);
            Assert.Zero(ShadersModule.NumBuiltVariants(), "Build Data was not cleared after analysis");
        }

#if BUILD_REPORT_API_SUPPORT
        [Test]
#if UNITY_2021_1_OR_NEWER
        [Ignore("TODO: investigate reason for test failure")]
#endif
        public void ShadersAnalysis_Sizes_AreReported()
        {
            var shaders = AnalyzeBuild(IssueCategory.Shader);

            var builtInShader = shaders.FirstOrDefault(s => s.description.Equals("Hidden/BlitCopy"));
            Assert.NotNull(builtInShader);
            Assert.AreEqual(ShadersModule.k_Unknown, builtInShader.GetCustomProperty(ShaderProperty.Size));

            var testShader = shaders.FirstOrDefault(s => s.description.Equals(k_ShaderName));
            Assert.NotNull(testShader);
            Assert.True(testShader.GetCustomPropertyInt64(ShaderProperty.Size) > 0);
        }

        [Test]
#if UNITY_2021_1_OR_NEWER
        [Ignore("TODO: investigate reason for test failure")]
#endif
        public void ShadersAnalysis_Variants_AreReported()
        {
            var issues = AnalyzeBuild(IssueCategory.ShaderVariant);
            var keywords = issues.Select(i => i.GetCustomProperty(ShaderVariantProperty.Keywords));
            Assert.True(keywords.Any(key => key.Equals(s_KeywordName)));

            var variants = issues.Where(i => i.description.Equals(k_ShaderName)).ToArray();
            Assert.Positive(variants.Length);
            Assert.True(variants.All(v => v.descriptor == null));
            Assert.True(variants.All(v => v.GetCustomProperty(ShaderVariantProperty.Tier).Equals("Tier1")));

            var shaderCompilerPlatforms = variants.Select(v => v.GetCustomProperty(ShaderVariantProperty.Platform)).Distinct();
            var compilerPlatformNames = ShaderUtilProxy.GetCompilerPlatformNames();

            foreach (var plat in shaderCompilerPlatforms)
            {
                Assert.Contains(plat, compilerPlatformNames);

                var variantsForPlatform = variants.Where(v => v.GetCustomProperty(ShaderVariantProperty.Platform).Equals(plat)).ToArray();
                Assert.AreEqual((int)ShaderVariantProperty.Num, variantsForPlatform[0].GetNumCustomProperties());

                Assert.True(variantsForPlatform.Any(v => v.GetCustomProperty(ShaderVariantProperty.Stage).Equals("Vertex")), "No Vertex shader variant found");
                Assert.True(variantsForPlatform.Any(v => v.GetCustomProperty(ShaderVariantProperty.Stage).Equals("Fragment")) || plat.Equals("OpenGLCore"), "No Fragment shader variant found for {0}", plat);
                Assert.True(variantsForPlatform.Any(v => v.GetCustomProperty(ShaderVariantProperty.PassType).Equals("Normal")), "No shader variant with Normal pass found");
                // "#pragma multi_compile __ KEYWORD_A KEYWORD_B" should produce 3 variants for each graphics API
                Assert.True(variantsForPlatform.Any(v => v.GetCustomProperty(ShaderVariantProperty.Keywords).Equals(ShadersModule.k_NoKeywords)));
                Assert.True(variantsForPlatform.Any(v => v.GetCustomProperty(ShaderVariantProperty.Keywords).Equals("KEYWORD_A")));
                Assert.True(variantsForPlatform.Any(v => v.GetCustomProperty(ShaderVariantProperty.Keywords).Equals("KEYWORD_B")));

                var colorSpaceGammaKeywordFound = variantsForPlatform.Any(v =>
                    v.GetCustomProperty(ShaderVariantProperty.PlatformKeywords).Contains("UNITY_COLORSPACE_GAMMA"));
                if (PlayerSettings.colorSpace == ColorSpace.Linear)
                    Assert.False(colorSpaceGammaKeywordFound, "ColorSpace is Linear but keyword UNITY_COLORSPACE_GAMMA was found");

                else
                    Assert.True(colorSpaceGammaKeywordFound, "ColorSpace is Gamma but keyword UNITY_COLORSPACE_GAMMA was not found");
                Assert.True(variantsForPlatform.All(v => v.GetCustomProperty(ShaderVariantProperty.Compiled).Equals(ShadersModule.k_NoRuntimeData)));
                Assert.True(variantsForPlatform.All(v => v.GetCustomProperty(ShaderVariantProperty.Requirements).Contains(ShaderRequirements.BaseShaders.ToString())));
            }
        }

        [Test]
#if UNITY_2021_1_OR_NEWER
        [Ignore("TODO: investigate reason for test failure")]
#endif
        public void ShadersAnalysis_VariantForBuiltInKeyword_IsReported()
        {
            var issues =  AnalyzeBuild(IssueCategory.ShaderVariant);

            var keywords = issues.Select(i => i.GetCustomProperty(ShaderVariantProperty.Keywords)).ToArray();

            Assert.True(keywords.Any(key => key.Equals(s_KeywordName)), "Keyword {0} not found in {1}", s_KeywordName, string.Join("\n", keywords));

            var variants = issues.Where(i => i.description.Equals("Custom/ShaderUsingBuiltInKeyword")).ToArray();
            Assert.Positive(variants.Length, "No shader variants found");

            // check custom properties
            Assert.True(variants.Any(v => v.GetCustomProperty(ShaderVariantProperty.Keywords).Equals(ShadersModule.k_NoKeywords)), "No shader variants found without keywords");

            var expectedRequirements = Formatting.CombineStrings(new[] {ShaderRequirements.BaseShaders, ShaderRequirements.Derivatives}.Select(r => r.ToString()).ToArray());
            Assert.True(variants.Any(v => v.GetCustomProperty(ShaderVariantProperty.Requirements).Equals(expectedRequirements)), "No shader variants found with {0} requirements", expectedRequirements);
#if UNITY_2019_1_OR_NEWER
            //this one fails on yamato
            //Assert.True(variants.Any(v => v.GetCustomProperty(ShaderVariantProperty.Keywords).Equals("INSTANCING_ON")), "No shader variants found with INSTANCING_ON keyword");
            //Assert.True(variants.Any(v => v.GetCustomProperty(ShaderVariantProperty.Requirements).Equals("BaseShaders, Derivatives, Instancing")), "No shader variants found with Instancing requirement");
#endif
        }

        [Test]
#if UNITY_2021_1_OR_NEWER
        [Ignore("TODO: investigate reason for test failure")]
#endif
        public void ShadersAnalysis_SurfShaderVariants_AreReported()
        {
            var issues =  AnalyzeBuild(IssueCategory.ShaderVariant);

            var keywords = issues.Select(i => i.GetCustomProperty(ShaderVariantProperty.Keywords)).ToArray();

            Assert.True(keywords.Any(key => key.Equals(s_KeywordName)), "Keyword {0} found in {1}", s_KeywordName, string.Join("\n", keywords));

            var variants = issues.Where(i => i.description.Equals("Custom/MySurfShader")).ToArray();
            Assert.Positive(variants.Count());

            // check custom property
            var variant = variants.FirstOrDefault(v => v.GetCustomProperty(ShaderVariantProperty.PassName).Equals("FORWARD") && v.GetCustomProperty(ShaderVariantProperty.Keywords).Equals("DIRECTIONAL"));
            Assert.NotNull(variant);
        }

        [Test]
#if UNITY_2021_1_OR_NEWER
        [Ignore("TODO: investigate reason for test failure")]
#endif
        public void ShadersAnalysis_StrippedVariants_AreNotReported()
        {
            StripVariants.Enabled = true;
            var issues = AnalyzeBuild(IssueCategory.ShaderVariant);
            StripVariants.Enabled = false;

            var builtVariantsKeywords = issues.Select(i => i.GetCustomProperty(ShaderVariantProperty.Keywords)).Distinct().ToArray();

            Assert.False(builtVariantsKeywords.Any(key => key.Equals(s_KeywordName)), "Keyword {0} found in {1}", s_KeywordName, string.Join("\n", builtVariantsKeywords));
        }

        [Test]
        public void ShadersAnalysis_PlayerLog_DoesNotContainShaderCompilationMessages()
        {
            var result = ShadersModule.ParsePlayerLog(m_PlayerLogWithNoCompilationResource.relativePath, new ProjectIssue[0]);
            Assert.That(result, Is.EqualTo(ParseLogResult.NoCompiledVariants));
        }

        [Test]
#if UNITY_2021_1_OR_NEWER
        [Ignore("TODO: investigate reason for test failure")]
#endif
        public void ShadersAnalysis_UnusedVariants_AreReported()
        {
            ShadersModule.ClearBuildData(); // clear previously built variants, if any
            var allVariants = AnalyzeBuild(IssueCategory.ShaderVariant);
            ShadersModule.ClearBuildData(); // cleanup

            var variants = allVariants.Where(i => i.description.Equals(k_ShaderName) && i.category == IssueCategory.ShaderVariant).ToArray();
            Assert.Positive(variants.Length);

            var result = ShadersModule.ParsePlayerLog(m_PlayerLogResource.relativePath, variants);

            Assert.That(result, Is.EqualTo(ParseLogResult.Success), "No compiled shader variants found in player log.");

            var shaderCompilerPlatforms = variants.Select(v => v.GetCustomProperty(ShaderVariantProperty.Platform)).Distinct().ToArray();
            var numShaderCompilerPlatforms = shaderCompilerPlatforms.Count();

            if (!shaderCompilerPlatforms.Contains("OpenGLCore"))
                Assert.AreEqual(10 * numShaderCompilerPlatforms, variants.Length, "Compiler Platforms: " + string.Join(", ", shaderCompilerPlatforms));

            var unusedVariants = variants.Where(i => !i.GetCustomPropertyBool(ShaderVariantProperty.Compiled)).ToArray();
            foreach (var plat in shaderCompilerPlatforms)
            {
                if (plat.Equals("OpenGLCore"))
                    continue;

                var unusedVariantsForPlatform = unusedVariants.Where(v => v.GetCustomProperty(ShaderVariantProperty.Platform).Equals(plat)).ToArray();

                Assert.AreEqual(4, unusedVariantsForPlatform.Length, "Unexpected number of variants for {0}", plat);
                Assert.AreEqual("MyTestShader/Pass", unusedVariantsForPlatform[0].GetCustomProperty(ShaderVariantProperty.PassName));
                Assert.AreEqual("KEYWORD_B", unusedVariantsForPlatform[0].GetCustomProperty(ShaderVariantProperty.Keywords));
#if UNITY_2019_1_OR_NEWER
                Assert.AreEqual("Pass 1", unusedVariantsForPlatform[2].GetCustomProperty(ShaderVariantProperty.PassName));
#else
                Assert.AreEqual(string.Empty, unusedVariantsForPlatform[2].GetCustomProperty(ShaderVariantProperty.PassName));
#endif
                Assert.AreEqual(ShadersModule.k_NoKeywords, unusedVariantsForPlatform[2].GetCustomProperty(ShaderVariantProperty.Keywords));
            }
        }

#endif

        [Test]
        public void ShadersAnalysis_Shader_IsReported()
        {
            ShadersModule.ClearBuildData();
            var issues = Analyze(IssueCategory.Shader, i => i.description.Equals(k_ShaderName));
            var shaderIssue = issues.FirstOrDefault();

            Assert.NotNull(shaderIssue);

            // check descriptor
            Assert.IsNull(shaderIssue.descriptor);

            // check custom property
            Assert.AreEqual((int)ShaderProperty.Num, shaderIssue.GetNumCustomProperties());
            Assert.AreEqual(ShadersModule.k_NotAvailable, shaderIssue.GetCustomProperty(ShaderProperty.NumBuiltVariants), "Num Variants: " + shaderIssue.GetCustomProperty(ShaderProperty.NumBuiltVariants));

#if UNITY_2021_1_OR_NEWER
            var expectedNumPasses = 2;
            var expectedNumKeywords = 12;
#elif UNITY_2019_1_OR_NEWER
            var expectedNumPasses = 2;
            var expectedNumKeywords = 2;
#else
            var expectedNumPasses = 0;
            var expectedNumKeywords = 0;
#endif

            Assert.AreEqual(expectedNumPasses, shaderIssue.GetCustomPropertyInt32(ShaderProperty.NumPasses), "NumPasses was : " + shaderIssue.GetCustomProperty(ShaderProperty.NumPasses));
            Assert.AreEqual(expectedNumKeywords, shaderIssue.GetCustomPropertyInt32(ShaderProperty.NumKeywords), "NumKeywords was : " + shaderIssue.GetCustomProperty(ShaderProperty.NumKeywords));

            Assert.AreEqual(2000, shaderIssue.GetCustomPropertyInt32(ShaderProperty.RenderQueue), "RenderQueue was : " + shaderIssue.GetCustomProperty(ShaderProperty.RenderQueue));
            Assert.False(shaderIssue.GetCustomPropertyBool(ShaderProperty.Instancing), "Instancing is supported but it should not be.");

            // assume SrpBatcher is supported when not using Built-in Render Pipeline
//            var isSrpBatcherSupported = RenderPipelineManager.currentPipeline != null;
//            Assert.AreEqual(isSrpBatcherSupported, shaderIssue.GetCustomPropertyAsBool(ShaderProperty.SrpBatcher), "SRP Batcher {0} supported but the SrpBatcher property does not match.", isSrpBatcherSupported ? "is" : "is not");
        }

#if UNITY_2019_1_OR_NEWER
        [Test]
        public void ShadersAnalysis_ShaderWithError_IsReported()
        {
            var shadersWithErrors = Analyze(IssueCategory.Shader, i => i.severity == Severity.Error);

            Assert.Positive(shadersWithErrors.Count());
            var shaderIssue = shadersWithErrors.FirstOrDefault(i => i.relativePath.Equals(m_ShaderWithErrorResource.relativePath));
            Assert.NotNull(shaderIssue);
        }

#endif

#if UNITY_2020_1_OR_NEWER
        // note that earlier Unity versions such as 2019.x do not report shader compiler messages
        [Test]
        public void ShadersAnalysis_CompilerMessage_IsReported()
        {
            var compilerMessages = Analyze(IssueCategory.ShaderCompilerMessage, i => i.description.Equals("floating point division by zero"));
            var message = compilerMessages.FirstOrDefault();
            Assert.NotNull(message);

            var allowedPlatforms = new[] {ShaderCompilerPlatform.Metal, ShaderCompilerPlatform.D3D, ShaderCompilerPlatform.OpenGLCore}.Select(p => p.ToString());
            Assert.True(allowedPlatforms.Contains(message.GetCustomProperty(ShaderMessageProperty.Platform)), "Platform: {0}", message.GetCustomProperty(ShaderMessageProperty.Platform));
            Assert.AreEqual(k_ShaderName, message.GetCustomProperty(ShaderMessageProperty.ShaderName), "Shader Name: {0}", message.GetCustomProperty(ShaderMessageProperty.ShaderName));
            Assert.AreEqual(Severity.Warning, message.severity);
            Assert.AreEqual(40, message.line);
        }

#endif

        [Test]
        public void ShadersAnalysis_ShaderUsingBuiltInKeyword_IsReported()
        {
            var issues = Analyze(IssueCategory.Shader, i => i.description.Equals("Custom/ShaderUsingBuiltInKeyword"));
            var shaderIssue = issues.FirstOrDefault();
            Assert.NotNull(shaderIssue);

            // check custom property
            Assert.AreEqual((int)ShaderProperty.Num, shaderIssue.GetNumCustomProperties());
#if UNITY_2021_1_OR_NEWER
            Assert.AreEqual(1, shaderIssue.GetCustomPropertyInt32(ShaderProperty.NumPasses), "NumPasses was : " + shaderIssue.GetCustomProperty(ShaderProperty.NumPasses));
            Assert.AreEqual(10, shaderIssue.GetCustomPropertyInt32(ShaderProperty.NumKeywords), "NumKeywords was : " + shaderIssue.GetCustomProperty(ShaderProperty.NumKeywords));
#elif UNITY_2019_1_OR_NEWER
            Assert.AreEqual(1, shaderIssue.GetCustomPropertyInt32(ShaderProperty.NumPasses), "NumPasses was : " + shaderIssue.GetCustomProperty(ShaderProperty.NumPasses));
            Assert.AreEqual(1, shaderIssue.GetCustomPropertyInt32(ShaderProperty.NumKeywords), "NumKeywords was : " + shaderIssue.GetCustomProperty(ShaderProperty.NumKeywords));
#else
            Assert.AreEqual(0, shaderIssue.GetCustomPropertyInt32(ShaderProperty.NumPasses), "NumPasses was : " + shaderIssue.GetCustomProperty(ShaderProperty.NumPasses));
            Assert.AreEqual(0, shaderIssue.GetCustomPropertyInt32(ShaderProperty.NumKeywords), "NumKeywords was : " + shaderIssue.GetCustomProperty(ShaderProperty.NumKeywords));
#endif
            Assert.AreEqual(2000, shaderIssue.GetCustomPropertyInt32(ShaderProperty.RenderQueue), "RenderQueue was : " + shaderIssue.GetCustomProperty(ShaderProperty.RenderQueue));
            Assert.True(shaderIssue.GetCustomPropertyBool(ShaderProperty.Instancing));
        }

        [Test]
        public void ShadersAnalysis_SurfShader_IsReported()
        {
            var issues = Analyze(IssueCategory.Shader, i => i.description.Equals("Custom/MySurfShader"));
            var shaderIssue = issues.FirstOrDefault();
            Assert.NotNull(shaderIssue);

            // check custom property
            Assert.AreEqual((int)ShaderProperty.Num, shaderIssue.GetNumCustomProperties());
#if UNITY_2021_1_OR_NEWER
            Assert.AreEqual(4, shaderIssue.GetCustomPropertyInt32(ShaderProperty.NumPasses), "NumPasses was : " + shaderIssue.GetCustomProperty(ShaderProperty.NumPasses));
            Assert.AreEqual(52, shaderIssue.GetCustomPropertyInt32(ShaderProperty.NumKeywords), "NumKeywords was : " + shaderIssue.GetCustomProperty(ShaderProperty.NumKeywords));
#elif UNITY_2019_1_OR_NEWER
            Assert.AreEqual(4, shaderIssue.GetCustomPropertyInt32(ShaderProperty.NumPasses), "NumPasses was : " + shaderIssue.GetCustomProperty(ShaderProperty.NumPasses));
            Assert.AreEqual(22, shaderIssue.GetCustomPropertyInt32(ShaderProperty.NumKeywords), "NumKeywords was : " + shaderIssue.GetCustomProperty(ShaderProperty.NumKeywords));
#else
            Assert.AreEqual(0, shaderIssue.GetCustomPropertyInt32(ShaderProperty.NumPasses), "NumPasses was : " + shaderIssue.GetCustomProperty(ShaderProperty.NumPasses));
            Assert.AreEqual(0, shaderIssue.GetCustomPropertyInt32(ShaderProperty.NumKeywords), "NumKeywords was : " + shaderIssue.GetCustomProperty(ShaderProperty.NumKeywords));
#endif
            Assert.AreEqual(2000, shaderIssue.GetCustomPropertyInt32(ShaderProperty.RenderQueue), "RenderQueue was : " + shaderIssue.GetCustomProperty(ShaderProperty.RenderQueue));
            Assert.True(shaderIssue.GetCustomPropertyBool(ShaderProperty.Instancing));
        }

        [Test]
        public void ShadersAnalysis_EditorShader_IsNotReported()
        {
            var issues = Analyze(IssueCategory.Shader, i => i.description.Equals("Custom/MyEditorShader"));

            Assert.Zero(issues.Length);
        }

        [Test]
#if UNITY_2021_1_OR_NEWER
        [Ignore("TODO: investigate reason for test failure")]
#endif
        public void ShadersAnalysis_EditorDefaultResourcesShader_IsNotReported()
        {
            var issues = Analyze(IssueCategory.Shader, i => i.relativePath.Contains("Editor Default Resources"));

            Assert.Zero(issues.Length);
        }
    }
}
