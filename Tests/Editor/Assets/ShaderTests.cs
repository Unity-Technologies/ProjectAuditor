using System.Linq;
using NUnit.Framework;
using Unity.ProjectAuditor.Editor;
using Unity.ProjectAuditor.Editor.Modules;
using Unity.ProjectAuditor.Editor.Tests.Common;
using UnityEditor.Rendering;

namespace Unity.ProjectAuditor.EditorTests
{
    class ShaderTests : TestFixtureBase
    {
        const string k_ShaderName = "Custom/MyTestShader,1"; // comma in the name for testing purposes

#pragma warning disable 0414
        TestAsset m_ShaderResource;
        TestAsset m_ShaderUsingBuiltInKeywordResource;
        TestAsset m_EditorShaderResource;

        TestAsset m_SurfShaderResource;

        TestAsset m_SrpBatchNonCompatibleShaderResource;
        TestAsset m_SrpBatchCompatibleShaderResource;
#pragma warning restore 0414

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

            m_SrpBatchNonCompatibleShaderResource = new TestAsset("Resources/SRPBatchNonCompatible.shader", @"
Shader ""Custom/SRPBatchNonCompatible""
            {
                Properties
                {
                    _Color1 (""Color 1"", Color) = (1,1,1,1)
                }
                SubShader
                {
                    Tags { ""RenderType"" = ""Opaque"" ""RenderPipeline"" = ""UniversalRenderPipeline"" }
                    Pass
                    {
                        HLSLPROGRAM
                        #pragma vertex vert
                        #pragma fragment frag
                        float4 _Color1;
                        struct Attributes
                        {
                            float4 positionOS   : POSITION;
                        };
                        struct Varyings
                        {
                            float4 positionHCS  : SV_POSITION;
                        };
                        Varyings vert(Attributes IN)
                        {
                            Varyings OUT;
                            OUT.positionHCS = IN.positionOS.xxyz;
                            return OUT;
                        }
                        half4 frag() : SV_Target
                        {
                            return _Color1;
                        }
                        ENDHLSL
                    }
                }
            }
");

            m_SrpBatchCompatibleShaderResource = new TestAsset("Resources/SRPBatchCompatible.shader", @"
Shader ""Custom/SRPBatchCompatible""
            {
                Properties
                {
                    _Color1 (""Color 1"", Color) = (1,1,1,1)
                }
                SubShader
                {
                    Tags { ""RenderType"" = ""Opaque"" ""RenderPipeline"" = ""UniversalRenderPipeline"" }
                    Pass
                    {
                        HLSLPROGRAM
                        #pragma vertex vert
                        #pragma fragment frag
                        struct Attributes
                        {
                            float4 positionOS   : POSITION;
                        };
                        struct Varyings
                        {
                            float4 positionHCS  : SV_POSITION;
                        };
                        Varyings vert(Attributes IN)
                        {
                            Varyings OUT;
                            OUT.positionHCS = IN.positionOS.xxyz;
                            return OUT;
                        }
                        half4 frag() : SV_Target
                        {
                            return half4(1, 1, 1, 1);
                        }
                        ENDHLSL
                    }
                }
            }
");
        }

        [Test]
        public void ShadersAnalysis_Shader_IsReported()
        {
            ShadersModule.ClearBuildData();
            var issues = Analyze(IssueCategory.Shader, i => i.Description.Equals(k_ShaderName));
            var shaderIssue = issues.FirstOrDefault();

            Assert.NotNull(shaderIssue);

            // check ID
            Assert.IsFalse(shaderIssue.Id.IsValid());

            // check custom property
            Assert.AreEqual((int)ShaderProperty.Num, shaderIssue.GetNumCustomProperties());
            Assert.AreEqual(ShadersModule.k_NotAvailable, shaderIssue.GetCustomProperty(ShaderProperty.NumBuiltVariants), "Num Variants: " + shaderIssue.GetCustomProperty(ShaderProperty.NumBuiltVariants));

#if UNITY_2021_1_OR_NEWER
            var expectedNumPasses = 2;
            var expectedNumKeywords = 12;
#else
            var expectedNumPasses = 2;
            var expectedNumKeywords = 2;
#endif

            Assert.AreEqual(expectedNumPasses, shaderIssue.GetCustomPropertyInt32(ShaderProperty.NumPasses), "NumPasses was : " + shaderIssue.GetCustomProperty(ShaderProperty.NumPasses));
            Assert.AreEqual(expectedNumKeywords, shaderIssue.GetCustomPropertyInt32(ShaderProperty.NumKeywords), "NumKeywords was : " + shaderIssue.GetCustomProperty(ShaderProperty.NumKeywords));

            Assert.AreEqual(2000, shaderIssue.GetCustomPropertyInt32(ShaderProperty.RenderQueue), "RenderQueue was : " + shaderIssue.GetCustomProperty(ShaderProperty.RenderQueue));
            Assert.False(shaderIssue.GetCustomPropertyBool(ShaderProperty.Instancing), "Instancing is supported but it should not be.");

            // assume SrpBatcher is supported when not using Built-in Render Pipeline
//            var isSrpBatcherSupported = RenderPipelineManager.currentPipeline != null;
//            Assert.AreEqual(isSrpBatcherSupported, shaderIssue.GetCustomPropertyAsBool(ShaderProperty.SrpBatcher), "SRP Batcher {0} supported but the SrpBatcher property does not match.", isSrpBatcherSupported ? "is" : "is not");
        }

        // note that earlier Unity versions such as 2019.x do not report shader compiler messages
        [Test]
        public void ShadersAnalysis_CompilerMessage_IsReported()
        {
            var compilerMessages = Analyze(IssueCategory.ShaderCompilerMessage, i => i.Description.Equals("floating point division by zero"));
            var message = compilerMessages.FirstOrDefault();
            Assert.NotNull(message);

            var allowedPlatforms = new[] {ShaderCompilerPlatform.Metal, ShaderCompilerPlatform.D3D, ShaderCompilerPlatform.OpenGLCore}.Select(p => p.ToString());
            Assert.True(allowedPlatforms.Contains(message.GetCustomProperty(ShaderMessageProperty.Platform)), "Platform: {0}", message.GetCustomProperty(ShaderMessageProperty.Platform));
            Assert.AreEqual(k_ShaderName, message.GetCustomProperty(ShaderMessageProperty.ShaderName), "Shader Name: {0}", message.GetCustomProperty(ShaderMessageProperty.ShaderName));
            Assert.AreEqual(Severity.Warning, message.Severity);
            Assert.AreEqual(40, message.Line);
        }

        [Test]
        public void ShadersAnalysis_ShaderUsingBuiltInKeyword_IsReported()
        {
            var issues = Analyze(IssueCategory.Shader, i => i.Description.Equals("Custom/ShaderUsingBuiltInKeyword"));
            var shaderIssue = issues.FirstOrDefault();
            Assert.NotNull(shaderIssue);

            // check custom property
            Assert.AreEqual((int)ShaderProperty.Num, shaderIssue.GetNumCustomProperties());

#if UNITY_2021_1_OR_NEWER
            var expectedNumPasses = 1;
            var expectedNumKeywords = 10;
#else
            var expectedNumPasses = 1;
            var expectedNumKeywords = 1;
#endif
            Assert.AreEqual(expectedNumPasses, shaderIssue.GetCustomPropertyInt32(ShaderProperty.NumPasses), "NumPasses was : " + shaderIssue.GetCustomProperty(ShaderProperty.NumPasses));
            Assert.AreEqual(expectedNumKeywords, shaderIssue.GetCustomPropertyInt32(ShaderProperty.NumKeywords), "NumKeywords was : " + shaderIssue.GetCustomProperty(ShaderProperty.NumKeywords));

            Assert.AreEqual(2000, shaderIssue.GetCustomPropertyInt32(ShaderProperty.RenderQueue), "RenderQueue was : " + shaderIssue.GetCustomProperty(ShaderProperty.RenderQueue));
            Assert.True(shaderIssue.GetCustomPropertyBool(ShaderProperty.Instancing));
        }

        [Test]
        public void ShadersAnalysis_SurfShader_IsReported()
        {
            var issues = Analyze(IssueCategory.Shader, i => i.Description.Equals("Custom/MySurfShader"));
            var shaderIssue = issues.FirstOrDefault();
            Assert.NotNull(shaderIssue);

            // check custom property
            Assert.AreEqual((int)ShaderProperty.Num, shaderIssue.GetNumCustomProperties());

#if UNITY_2021_1_OR_NEWER
            var expectedNumPasses = 4;
            var expectedNumKeywords = 52;
#else
            var expectedNumPasses = 4;
            var expectedNumKeywords = 22;
#endif
            Assert.AreEqual(expectedNumPasses, shaderIssue.GetCustomPropertyInt32(ShaderProperty.NumPasses), "NumPasses was : " + shaderIssue.GetCustomProperty(ShaderProperty.NumPasses));
            Assert.AreEqual(expectedNumKeywords, shaderIssue.GetCustomPropertyInt32(ShaderProperty.NumKeywords), "NumKeywords was : " + shaderIssue.GetCustomProperty(ShaderProperty.NumKeywords));

            Assert.AreEqual(2000, shaderIssue.GetCustomPropertyInt32(ShaderProperty.RenderQueue), "RenderQueue was : " + shaderIssue.GetCustomProperty(ShaderProperty.RenderQueue));
            Assert.True(shaderIssue.GetCustomPropertyBool(ShaderProperty.Instancing));
        }

        [Test]
        public void ShadersAnalysis_EditorShader_IsNotReported()
        {
            var issues = Analyze(IssueCategory.Shader, i => i.Description.Equals("Custom/MyEditorShader"));

            Assert.Zero(issues.Length);
        }

        [Test]
        public void ShadersAnalysis_EditorDefaultResourcesShader_IsNotReported()
        {
            var issues = Analyze(IssueCategory.Shader, i => i.RelativePath.Contains("Editor Default Resources"));

            Assert.Zero(issues.Length);
        }

        [Test]
        public void ShadersAnalysis_SRPNonCompatibleShader_IsReported()
        {
            // TODO: need to setup yamato so we add and use SRP to the test project
            if (!ShaderAnalyzer.IsSrpBatchingEnabled)
            {
                return;
            }

            var issues = AnalyzeAndFindAssetIssues(m_SrpBatchNonCompatibleShaderResource, IssueCategory.AssetIssue);

            Assert.IsNotEmpty(issues);
            Assert.IsTrue(issues.Any(issue => issue.Id == ShaderAnalyzer.PAA2000),
                "The not compatible with SRP batcher shader should be reported.");
        }

        [Test]
        public void ShadersAnalysis_SRPCompatibleShader_IsNotReported()
        {
            // TODO: need to setup yamato so we add and use SRP to the test project
            if (!ShaderAnalyzer.IsSrpBatchingEnabled)
            {
                return;
            }

            var issues = AnalyzeAndFindAssetIssues(m_SrpBatchCompatibleShaderResource, IssueCategory.AssetIssue);

            Assert.IsFalse(issues.Any(issue => issue.Id == ShaderAnalyzer.PAA2000),
                "The compatible with SRP batcher shader should not be reported.");
        }
    }
}
