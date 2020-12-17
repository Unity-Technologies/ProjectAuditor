using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Unity.ProjectAuditor.Editor;
using Unity.ProjectAuditor.Editor.Auditors;
using UnityEditor.Build;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEditor.ProjectAuditor.EditorTests
{
    class ShaderTests
    {
        TempAsset m_ShaderResource;
        TempAsset m_EditorShaderResource;

#if UNITY_2018_2_OR_NEWER
        private static string s_KeywordName = "DIRECTIONAL";

        class StripVariants : IPreprocessShaders
        {
            static public bool Enabled = false;
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
Shader ""Custom/MyTestShader""
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
            ShadersAuditor.CleanupBuildData();
            var projectAuditor = new Unity.ProjectAuditor.Editor.ProjectAuditor();

            var projectReport = projectAuditor.Audit();
            var issues = projectReport.GetIssues(IssueCategory.ShaderVariants);
            Assert.Positive(issues.Length);
            Assert.True(issues.First().description.Equals("Build the project to view the Shader Variants"));
        }

        [Test]
        public void ShaderVariantsAreReported()
        {
            var issues = BuildAndAnalyze();

            var keywords = issues.Select(i => i.GetCustomProperty((int)ShaderVariantProperty.Keywords));

            Assert.True(keywords.Any(key => key.Equals(s_KeywordName)));
        }

        [Test]
        public void StrippedVariantsAreNotReported()
        {
            StripVariants.Enabled = true;
            var issues = BuildAndAnalyze();
            StripVariants.Enabled = false;

            var keywords = issues.Select(i => i.GetCustomProperty((int)ShaderVariantProperty.Keywords));

            Assert.False(keywords.Any(key => key.Equals(s_KeywordName)));
        }

        private static ProjectIssue[] BuildAndAnalyze()
        {
            var buildPath = FileUtil.GetUniqueTempPathInProject();
            Directory.CreateDirectory(buildPath);
            var buildPlayerOptions = new BuildPlayerOptions
            {
                scenes = new string[] {},
                locationPathName = Path.Combine(buildPath, "test"),
                target = EditorUserBuildSettings.activeBuildTarget,
                targetGroup = BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget),
                options = BuildOptions.Development
            };
            var buildReport = BuildPipeline.BuildPlayer(buildPlayerOptions);

            Assert.True(buildReport.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded);

            Directory.Delete(buildPath, true);

            var projectAuditor = new Unity.ProjectAuditor.Editor.ProjectAuditor();
            var projectReport = projectAuditor.Audit();
            var issues = projectReport.GetIssues(IssueCategory.ShaderVariants);
            issues = issues.Where(i => i.description.Equals("Custom/MyTestShader")).ToArray();

            Assert.Positive(issues.Length);
            return issues;
        }

#endif

        [Test]
        public void ShaderIsReported()
        {
            var projectAuditor = new Unity.ProjectAuditor.Editor.ProjectAuditor();
            var projectReport = projectAuditor.Audit();
            var issues = projectReport.GetIssues(IssueCategory.Shaders);
            var descriptions = issues.Select(i => i.description).ToArray();

            Assert.Contains("Custom/MyTestShader", descriptions);
        }

        [Test]
        public void EditorShaderIsNotReported()
        {
            var projectAuditor = new Unity.ProjectAuditor.Editor.ProjectAuditor();

            var projectReport = projectAuditor.Audit();
            var issues = projectReport.GetIssues(IssueCategory.Shaders);
            issues = issues.Where(i => i.description.Equals("Custom/MyEditorShader")).ToArray();

            Assert.Zero(issues.Length);
        }
    }
}
