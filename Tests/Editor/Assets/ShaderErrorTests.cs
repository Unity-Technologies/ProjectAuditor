using System.Collections;
using System.Linq;
using NUnit.Framework;
using Unity.ProjectAuditor.Editor;
using Unity.ProjectAuditor.Editor.Modules;
using Unity.ProjectAuditor.Editor.Tests.Common;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace Unity.ProjectAuditor.EditorTests
{
    class ShaderErrorTests : TestFixtureBase
    {
        // Shader compile failure messaging is inconsistent at best prior to 2020 versions.
        [SetUp]
        public void Clear()
        {
            // Clear previously built variants
            ShadersModule.ClearBuildData();
        }

        [UnityTest]
#if UNITY_2022_3_OR_NEWER && UNITY_EDITOR_WIN
        [Ignore("This fails with error: unexpected token '}' at line 38 (on d3d11)")]
#endif
        public IEnumerator ShadersAnalysis_ShaderWithFunctionError_IsReported()
        {
            // Make this one a regex because the error message includes a line number and graphics API, neither of which I'm sure we should be relying on.
            UnityEngine.TestTools.LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("(Shader error in 'Custom/ShaderWithFunctionError': syntax error: unexpected token '}')(.)+"));
            var local_shaderWithFunctionError = new TestAsset("Resources/ShaderWithFunctionError.shader", @"
            Shader ""Custom/ShaderWithFunctionError""
            {
                SubShader
                {
                    Pass
                    {
                        CGPROGRAM
    #pragma vertex vert
    #pragma fragment frag

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
                            return tex2D(_MainTex, i.uv) // Missing semicolon
                        }
                        ENDCG
                    }
                }
            }");

            while (ShaderUtil.anythingCompiling)
            {
                yield return null;
            }

            var shadersWithErrors = Analyze(IssueCategory.Shader, i => i.Severity == Severity.Error);

            Assert.Positive(shadersWithErrors.Count());
            var shaderIssue = shadersWithErrors.FirstOrDefault(i => i.RelativePath.Equals(local_shaderWithFunctionError.RelativePath));
            Assert.NotNull(shaderIssue);

            local_shaderWithFunctionError.CleanupLocal();
        }

        [UnityTest]
        public IEnumerator ShadersAnalysis_ShaderWithShaderLabError_IsReported()
        {
#if UNITY_2021_1_OR_NEWER
            UnityEngine.TestTools.LogAssert.Expect(LogType.Error, "Shader error in '': Parse error: syntax error, unexpected TVAL_ID, expecting TOK_SHADER at line 2");
#endif
            var local_shaderWithShaderLabError = new TestAsset("Resources/ShaderWithShaderLabError.shader", @"
            Sader ""Custom/ShaderWithShaderLabError""
            {
            }");

            while (ShaderUtil.anythingCompiling)
            {
                yield return null;
            }

            var shadersWithErrors = Analyze(IssueCategory.Shader, i => i.Severity == Severity.Error);

            Assert.Positive(shadersWithErrors.Count());
            var shaderIssue = shadersWithErrors.FirstOrDefault(i => i.RelativePath.Equals(local_shaderWithShaderLabError.RelativePath));
            Assert.NotNull(shaderIssue);

            local_shaderWithShaderLabError.CleanupLocal();
        }
    }
}
