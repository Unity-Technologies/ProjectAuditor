using System.Linq;
using System.Reflection;
using NUnit.Framework;
using Unity.ProjectAuditor.Editor;
using Unity.ProjectAuditor.Editor.SettingsAnalysis;
using Unity.ProjectAuditor.Editor.Tests.Common;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;
using FogMode = Unity.ProjectAuditor.Editor.SettingsAnalysis.FogMode;

namespace Unity.ProjectAuditor.EditorTests
{
    class GraphicsSettingsTests : TestFixtureBase
    {
        [TestCase(false)]
        [TestCase(true)]
        public void SettingsAnalysis_GraphicsMixedStandardShaderQuality_WithBuiltinRenderPipeline_IsReported(bool isMixed)
        {
            var buildTarget = m_Platform;
            var buildTargetGroup = BuildPipeline.GetBuildTargetGroup(m_Platform);
            var savedTier1settings = EditorGraphicsSettings.GetTierSettings(buildTargetGroup, GraphicsTier.Tier1);
            var savedTier2settings = EditorGraphicsSettings.GetTierSettings(buildTargetGroup, GraphicsTier.Tier2);
            var savedTier3settings = EditorGraphicsSettings.GetTierSettings(buildTargetGroup, GraphicsTier.Tier3);

            var tier1settings = EditorGraphicsSettings.GetTierSettings(buildTargetGroup, GraphicsTier.Tier1);
            var tier2settings = EditorGraphicsSettings.GetTierSettings(buildTargetGroup, GraphicsTier.Tier2);
            var tier3settings = EditorGraphicsSettings.GetTierSettings(buildTargetGroup, GraphicsTier.Tier3);
            var defaultRenderPipeline = GraphicsSettings.defaultRenderPipeline;

            tier1settings.standardShaderQuality = ShaderQuality.High;
            tier2settings.standardShaderQuality = ShaderQuality.High;
            tier3settings.standardShaderQuality = isMixed ? ShaderQuality.Low : ShaderQuality.High;

            EditorGraphicsSettings.SetTierSettings(buildTargetGroup, GraphicsTier.Tier1, tier1settings);
            EditorGraphicsSettings.SetTierSettings(buildTargetGroup, GraphicsTier.Tier2, tier2settings);
            EditorGraphicsSettings.SetTierSettings(buildTargetGroup, GraphicsTier.Tier3, tier3settings);

            GraphicsSettings.defaultRenderPipeline = null;

            Assert.AreEqual(isMixed, BuiltinRenderPipelineAnalyzer.IsMixedStandardShaderQuality(buildTarget));

            EditorGraphicsSettings.SetTierSettings(buildTargetGroup, GraphicsTier.Tier1, savedTier1settings);
            EditorGraphicsSettings.SetTierSettings(buildTargetGroup, GraphicsTier.Tier2, savedTier2settings);
            EditorGraphicsSettings.SetTierSettings(buildTargetGroup, GraphicsTier.Tier3, savedTier3settings);

            GraphicsSettings.defaultRenderPipeline = defaultRenderPipeline;
        }

        [TestCase(RenderingPath.Forward)]
        [TestCase(RenderingPath.DeferredShading)]
        public void SettingsAnalysis_GraphicsUsingRenderingPath_WithBuiltinRenderPipeline_IsReported(RenderingPath renderingPath)
        {
            var buildTarget = m_Platform;
            var buildTargetGroup = BuildPipeline.GetBuildTargetGroup(m_Platform);
            var savedTier1settings = EditorGraphicsSettings.GetTierSettings(buildTargetGroup, GraphicsTier.Tier1);
            var savedTier2settings = EditorGraphicsSettings.GetTierSettings(buildTargetGroup, GraphicsTier.Tier2);
            var savedTier3settings = EditorGraphicsSettings.GetTierSettings(buildTargetGroup, GraphicsTier.Tier3);

            var tier1settings = EditorGraphicsSettings.GetTierSettings(buildTargetGroup, GraphicsTier.Tier1);
            var tier2settings = EditorGraphicsSettings.GetTierSettings(buildTargetGroup, GraphicsTier.Tier2);
            var tier3settings = EditorGraphicsSettings.GetTierSettings(buildTargetGroup, GraphicsTier.Tier3);
            var defaultRenderPipeline = GraphicsSettings.defaultRenderPipeline;

            tier1settings.renderingPath = renderingPath;
            tier2settings.renderingPath = renderingPath;
            tier3settings.renderingPath = renderingPath;

            EditorGraphicsSettings.SetTierSettings(buildTargetGroup, GraphicsTier.Tier1, tier1settings);
            EditorGraphicsSettings.SetTierSettings(buildTargetGroup, GraphicsTier.Tier2, tier2settings);
            EditorGraphicsSettings.SetTierSettings(buildTargetGroup, GraphicsTier.Tier3, tier3settings);

            GraphicsSettings.defaultRenderPipeline = null;

            if (renderingPath == RenderingPath.Forward)
            {
                Assert.AreEqual(true, BuiltinRenderPipelineAnalyzer.IsUsingForwardRendering(buildTarget));
                Assert.AreEqual(false, BuiltinRenderPipelineAnalyzer.IsUsingDeferredRendering(buildTarget));
            }
            else
            {
                Assert.AreEqual(false, BuiltinRenderPipelineAnalyzer.IsUsingForwardRendering(buildTarget));
                Assert.AreEqual(true, BuiltinRenderPipelineAnalyzer.IsUsingDeferredRendering(buildTarget));
            }

            EditorGraphicsSettings.SetTierSettings(buildTargetGroup, GraphicsTier.Tier1, savedTier1settings);
            EditorGraphicsSettings.SetTierSettings(buildTargetGroup, GraphicsTier.Tier2, savedTier2settings);
            EditorGraphicsSettings.SetTierSettings(buildTargetGroup, GraphicsTier.Tier3, savedTier3settings);

            GraphicsSettings.defaultRenderPipeline = defaultRenderPipeline;
        }

        [Test]
        [TestCase(FogMode.Exponential)]
        [TestCase(FogMode.ExponentialSquared)]
        [TestCase(FogMode.Linear)]
        public void SettingsAnalysis_FogStripping_IsReported(FogMode fogMode)
        {
            var graphicsSettings = GraphicsSettings.GetGraphicsSettings();
            var serializedObject = new SerializedObject(graphicsSettings);

            var fogStrippingProperty = serializedObject.FindProperty("m_FogStripping");
            var fogStripping = fogStrippingProperty.enumValueIndex;

            fogStrippingProperty.enumValueIndex = (int)FogStripping.Custom;

            var linearFogModeProperty = serializedObject.FindProperty("m_FogKeepLinear");
            var expFogModeProperty = serializedObject.FindProperty("m_FogKeepExp");
            var exp2FogModeProperty = serializedObject.FindProperty("m_FogKeepExp2");

            var linearEnabled = linearFogModeProperty.boolValue;
            var expEnabled = expFogModeProperty.boolValue;
            var exp2Enabled = exp2FogModeProperty.boolValue;

            expFogModeProperty.boolValue = false;
            linearFogModeProperty.boolValue = false;
            exp2FogModeProperty.boolValue = false;

            switch (fogMode)
            {
                case FogMode.Exponential:
                    expFogModeProperty.boolValue = true;
                    break;

                case FogMode.ExponentialSquared:
                    exp2FogModeProperty.boolValue = true;
                    break;

                case FogMode.Linear:
                    linearFogModeProperty.boolValue = true;
                    break;
            }

            serializedObject.ApplyModifiedProperties();
            Assert.IsTrue(FogStrippingAnalyzer.IsFogModeEnabled(fogMode));

            var issues = Analyze(IssueCategory.ProjectSetting, i => i.Id.Equals(FogStrippingAnalyzer.PAS1003));

            Assert.AreEqual(1, issues.Length);

            var description = $"Graphics: Fog Mode '{fogMode}' shader variants are always included in the build";
            Assert.AreEqual(description, issues[0].Description);

            linearFogModeProperty.boolValue = linearEnabled;
            expFogModeProperty.boolValue = expEnabled;
            exp2FogModeProperty.boolValue = exp2Enabled;

            fogStrippingProperty.enumValueIndex = fogStripping;

            serializedObject.ApplyModifiedProperties();
        }

        [Test]
        [TestCase(FogStripping.Automatic)]
        [TestCase(FogStripping.Custom)]
        public void SettingsAnalysis_FogStripping_IsNotReported(FogStripping fogModeStripping)
        {
            var graphicsSettings = GraphicsSettings.GetGraphicsSettings();
            var serializedObject = new SerializedObject(graphicsSettings);

            var fogStrippingProperty = serializedObject.FindProperty("m_FogStripping");
            var fogStripping = fogStrippingProperty.enumValueIndex;

            var linearFogModeProperty = serializedObject.FindProperty("m_FogKeepLinear");
            var expFogModeProperty = serializedObject.FindProperty("m_FogKeepExp");
            var exp2FogModeProperty = serializedObject.FindProperty("m_FogKeepExp2");

            var linearEnabled = linearFogModeProperty.boolValue;
            var expEnabled = expFogModeProperty.boolValue;
            var exp2Enabled = exp2FogModeProperty.boolValue;

            fogStrippingProperty.enumValueIndex = (int)fogModeStripping;

            if (fogModeStripping == FogStripping.Custom)
            {
                linearFogModeProperty.boolValue = false;
                expFogModeProperty.boolValue = false;
                exp2FogModeProperty.boolValue = false;
            }

            serializedObject.ApplyModifiedProperties();

            var issues = Analyze(IssueCategory.ProjectSetting, i => i.Id.Equals(FogStrippingAnalyzer.PAS1003));
            var playerSettingIssue = issues.FirstOrDefault();

            Assert.IsNull(playerSettingIssue);

            fogStrippingProperty.enumValueIndex = fogStripping;

            linearFogModeProperty.boolValue = linearEnabled;
            expFogModeProperty.boolValue = expEnabled;
            exp2FogModeProperty.boolValue = exp2Enabled;

            serializedObject.ApplyModifiedProperties();
        }

        #if !PACKAGE_HYBRID_RENDERER && !PACKAGE_ENTITIES_GRAPHICS
        [Ignore("This requires the Hybrid Renderer or Entities Graphics package")]
#endif
        [Test]
        public void SettingsAnalysis_Default_StaticBatching_Enabled_IsReported()
        {
            SetupStaticBatchingGetterAndSetter(
                out var getterMethod, out var setterMethod,
                out var getterArgs, out var setterArgs,
                1);

            setterMethod.Invoke(null, setterArgs);

            var id = new DescriptorId();
#if PACKAGE_ENTITIES_GRAPHICS
            id = EntitiesGraphicsAnalyzer.PAS1013;
#elif PACKAGE_HYBRID_RENDERER
            id = EntitiesGraphicsAnalyzer.PAS1000;
#endif

            var issues = Analyze(IssueCategory.ProjectSetting, i => i.Id.Equals(id));
            Assert.True(issues.Length == 1);

            // Test Fixer
            issues[0].Id.GetDescriptor().Fix(issues[0], m_AnalysisParams);
            var issuesAfterFix = Analyze(IssueCategory.ProjectSetting, i => i.Id.Equals(id));

            setterMethod.Invoke(null, getterArgs);

            Assert.True(issuesAfterFix.Length == 0);
        }

#if !PACKAGE_HYBRID_RENDERER && !PACKAGE_ENTITIES_GRAPHICS
        [Ignore("This requires the Hybrid Renderer or Entities Graphics package")]
#endif
        [Test]
        public void SettingsAnalysis_StaticBatching_Disabled_IsNotReported()
        {
            SetupStaticBatchingGetterAndSetter(
                out var getterMethod, out var setterMethod,
                out var getterArgs, out var setterArgs,
                0);
            setterMethod.Invoke(null, setterArgs);

            var id = new DescriptorId();
#if PACKAGE_ENTITIES_GRAPHICS
            id = EntitiesGraphicsAnalyzer.PAS1013;
#elif PACKAGE_HYBRID_RENDERER
            id = EntitiesGraphicsAnalyzer.PAS1000;
#endif

            var issues = Analyze(IssueCategory.ProjectSetting, i => i.Id.Equals(id));

            setterMethod.Invoke(null, getterArgs);

            Assert.True(issues.Length == 0);
        }

        void SetupStaticBatchingGetterAndSetter(out MethodInfo getterMethod, out MethodInfo setterMethod,
            out object[] getterArgs, out object[] setterArgs, int staticBatchingForTest)
        {
            getterMethod = typeof(PlayerSettings).GetMethod("GetBatchingForPlatform",
                BindingFlags.Static | BindingFlags.Default | BindingFlags.NonPublic);

            setterMethod = typeof(PlayerSettings).GetMethod("SetBatchingForPlatform",
                BindingFlags.Static | BindingFlags.Default | BindingFlags.NonPublic);

            Assert.True(getterMethod != null, "GetBatchingForPlatform method does not exist");
            Assert.True(setterMethod != null, "SetBatchingForPlatform method does not exist");

            const int initialStaticBatching = 0;
            const int initialDynamicBatching = 0;
            getterArgs = new object[]
            {
                m_Platform,
                initialStaticBatching,
                initialDynamicBatching
            };

            getterMethod.Invoke(null, getterArgs);

            int staticBatching = staticBatchingForTest;
            const int dynamicBatching = 0;
            setterArgs = new object[]
            {
                m_Platform,
                staticBatching,
                dynamicBatching
            };
        }
    }
}
