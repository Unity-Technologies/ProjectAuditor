using System.IO;
using System.Linq;
using NUnit.Framework;
using Unity.ProjectAuditor.Editor;

namespace UnityEditor.ProjectAuditor.EditorTests
{
    class ShaderTests
    {
        ScriptResource m_ShaderResource;

        [OneTimeSetUp]
        public void SetUp()
        {
            m_ShaderResource = new ScriptResource("Resources/MyTestShader.shader", @"
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
        }

        [OneTimeTearDown]
        public void TearDown()
        {
            m_ShaderResource.Delete();
        }

        [Test]
        public void BuildIsRequired()
        {
            var projectAuditor = new Unity.ProjectAuditor.Editor.ProjectAuditor();

            var projectReport = projectAuditor.Audit();
            var issues = projectReport.GetIssues(IssueCategory.Shaders);

            Assert.Positive(issues.Length);
            Assert.True(issues.First().description.Equals("Build the project and run Project Auditor analysis"));
        }

        [Test]
        public void ShaderVariantsAreReported()
        {
            var targetPath = FileUtil.GetUniqueTempPathInProject();
            Directory.CreateDirectory(targetPath);
            var buildPlayerOptions = new BuildPlayerOptions
            {
                scenes = new string[] {},
                locationPathName = targetPath,
                target = EditorUserBuildSettings.activeBuildTarget,
                options = BuildOptions.Development
            };
            var buildReport = BuildPipeline.BuildPlayer(buildPlayerOptions);

            var projectAuditor = new Unity.ProjectAuditor.Editor.ProjectAuditor();

            var projectReport = projectAuditor.Audit();
            var issues = projectReport.GetIssues(IssueCategory.Shaders);
            issues = issues.Where(i => i.description.Equals("Custom/MyTestShader")).ToArray();
            Assert.AreEqual(42, issues.Length);

            Directory.Delete(targetPath);
        }
    }
}
