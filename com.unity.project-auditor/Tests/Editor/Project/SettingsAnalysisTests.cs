using System.Linq;
using System.Reflection;
using NUnit.Framework;
using Unity.ProjectAuditor.Editor;
using Unity.ProjectAuditor.Editor.Core;
using Unity.ProjectAuditor.Editor.SettingsAnalysis;
using Unity.ProjectAuditor.Editor.Tests.Common;
using Unity.ProjectAuditor.Editor.Utils;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEditor.TestTools;
using UnityEngine;
using UnityEngine.Rendering;
using FogMode = Unity.ProjectAuditor.Editor.SettingsAnalysis.FogMode;


namespace Unity.ProjectAuditor.EditorTests
{
    class SettingsAnalysisTests : TestFixtureBase
    {
        [Test]
        [RequirePlatformSupport(BuildTarget.iOS)]
        public void SettingsAnalysis_Default_AccelerometerFrequency_IsReported()
        {
            var accelerometerFrequency = PlayerSettings.accelerometerFrequency;
            var platform = m_Platform;

            PlayerSettings.accelerometerFrequency = 1;
            m_Platform = BuildTarget.iOS;

            var issues = Analyze(IssueCategory.ProjectSetting, i => i.Id.Equals(PlayerSettingsAnalyzer.PAS0002));

            m_Platform = platform;
            PlayerSettings.accelerometerFrequency = accelerometerFrequency;

            Assert.True(issues.Length == 1);
        }

        [Test]
        [RequirePlatformSupport(BuildTarget.iOS)]
        public void SettingsAnalysis_Disabled_AccelerometerFrequency_IsNotReported()
        {
            var accelerometerFrequency = PlayerSettings.accelerometerFrequency;
            var platform = m_Platform;

            PlayerSettings.accelerometerFrequency = 0;
            m_Platform = BuildTarget.iOS;

            var issues = Analyze(IssueCategory.ProjectSetting, i => i.Id.Equals(PlayerSettingsAnalyzer.PAS0002));

            m_Platform = platform;
            PlayerSettings.accelerometerFrequency = accelerometerFrequency;

            Assert.True(issues.Length == 0);
        }

        [Test]
        public void SettingsAnalysis_Default_PhysicsLayerCollisionMatrix_IsReported()
        {
            const int numLayers = 32;
            var oldValues = new bool[528];

            int count = 0;
            for (var i = 0; i < numLayers; ++i)
                for (var j = i; j < numLayers; ++j)
                    oldValues[count++] = Physics.GetIgnoreLayerCollision(i, j);

            for (var i = 0; i < numLayers; ++i)
                for (var j = i; j < numLayers; ++j)
                    Physics.IgnoreLayerCollision(i, j, false);

            var issues = Analyze(IssueCategory.ProjectSetting, i => i.Id.Equals(PhysicsAnalyzer.PAS0013));

            count = 0;
            for (var i = 0; i < numLayers; ++i)
                for (var j = i; j < numLayers; ++j)
                    Physics.IgnoreLayerCollision(i, j, oldValues[count++]);

            Assert.True(issues.Length == 1);
        }

        [Test]
        public void SettingsAnalysis_NonDefault_PhysicsLayerCollisionMatrix_IsNotReported()
        {
            var oldValue = Physics.GetIgnoreLayerCollision(0, 0);

            Physics.IgnoreLayerCollision(0, 0, true);

            var issues = Analyze(IssueCategory.ProjectSetting, i => i.Id.Equals(PhysicsAnalyzer.PAS0013));

            Physics.IgnoreLayerCollision(0, 0, oldValue);

            Assert.True(issues.Length == 0);
        }

        [Test]
        public void SettingsAnalysis_Default_Physics2DLayerCollisionMatrix_IsReported()
        {
            const int numLayers = 32;
            var oldValues = new bool[528];

            int count = 0;
            for (var i = 0; i < numLayers; ++i)
                for (var j = i; j < numLayers; ++j)
                    oldValues[count++] = Physics2D.GetIgnoreLayerCollision(i, j);

            for (var i = 0; i < numLayers; ++i)
                for (var j = i; j < numLayers; ++j)
                    Physics2D.IgnoreLayerCollision(i, j, false);

            var issues = Analyze(IssueCategory.ProjectSetting, i => i.Id.Equals(Physics2DAnalyzer.PAS0015));

            count = 0;
            for (var i = 0; i < numLayers; ++i)
                for (var j = i; j < numLayers; ++j)
                    Physics2D.IgnoreLayerCollision(i, j, oldValues[count++]);

            Assert.True(issues.Length == 1);
        }

        [Test]
        public void SettingsAnalysis_NonDefault_Physics2DLayerCollisionMatrix_IsNotReported()
        {
            var oldValue = Physics2D.GetIgnoreLayerCollision(0, 0);

            Physics2D.IgnoreLayerCollision(0, 0, true);

            var issues = Analyze(IssueCategory.ProjectSetting, i => i.Id.Equals(Physics2DAnalyzer.PAS0015));

            Physics2D.IgnoreLayerCollision(0, 0, oldValue);

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

        [Test]
        public void SettingsAnalysis_Issue_IsReported()
        {
            var savedSetting = PlayerSettings.bakeCollisionMeshes;
            PlayerSettings.bakeCollisionMeshes = false;

            var issues = Analyze(IssueCategory.ProjectSetting, i =>
                i.Id.IsValid() && i.Id.GetDescriptor().Method.Equals("bakeCollisionMeshes"));

            var playerSettingIssue = issues.FirstOrDefault();
            var descriptor = playerSettingIssue.Id.GetDescriptor();

            Assert.NotNull(playerSettingIssue, "Issue not found");
            Assert.AreEqual("Player: Prebake Collision Meshes is disabled", playerSettingIssue.Description);
            Assert.AreEqual("Project/Player", playerSettingIssue.Location.Path);
            Assert.AreEqual("Player", playerSettingIssue.Location.Filename);
            Assert.AreEqual((Areas.BuildSize | Areas.LoadTime), descriptor.Areas);
            Assert.AreEqual("Any", descriptor.GetPlatformsSummary());

            // restore bakeCollisionMeshes
            PlayerSettings.bakeCollisionMeshes = savedSetting;
        }

        [Test]
        public void SettingsAnalysis_Issue_IsNotReportedOnceFixed()
        {
            var savedFixedDeltaTime = Time.fixedDeltaTime;
            // 0.02f is the default Time.fixedDeltaTime value and will be reported as an issue
            Time.fixedDeltaTime = 0.02f;

            var issues = Analyze(IssueCategory.ProjectSetting, i => i.Id.Equals(TimeSettingsAnalyzer.PAS0016));
            var playerSettingIssue = issues.FirstOrDefault();
            Assert.NotNull(playerSettingIssue, "Issue not found");
            Assert.AreEqual("Time: Fixed Timestep is set to the default value", playerSettingIssue.Description);
            Assert.AreEqual("Project/Time", playerSettingIssue.Location.Path);

            // "fix" fixedDeltaTime so it's not reported anymore
            Time.fixedDeltaTime = 0.021f;

            issues = Analyze(IssueCategory.ProjectSetting, i => i.Id.Equals(TimeSettingsAnalyzer.PAS0016));
            Assert.Null(issues.FirstOrDefault());

            // restore Time.fixedDeltaTime
            Time.fixedDeltaTime = savedFixedDeltaTime;
        }

        [Test]
        [TestCase(false)]
        [TestCase(true)]
        public void SettingsAnalysis_SplashScreen_IsEnabledAndCanBeDisabled(bool splashScreenEnabled)
        {
            var prevSplashScreenEnabled = PlayerSettings.SplashScreen.show;
            PlayerSettings.SplashScreen.show = splashScreenEnabled;

            Assert.AreEqual(splashScreenEnabled, PlayerSettingsAnalyzer.IsSplashScreenEnabledAndCanBeDisabled());

            PlayerSettings.SplashScreen.show = prevSplashScreenEnabled;
        }

        [Test]
        [RequirePlatformSupport(BuildTarget.Android)]
        public void SettingsAnalysis_AudioMode_SpeakerModeStereo_IsReported()
        {
            var oldPlatform = m_Platform;
            m_Platform = BuildTarget.Android;

            var audioConfiguration = AudioSettings.GetConfiguration();
            AudioSettings.speakerMode = AudioSpeakerMode.Stereo;

            var issues = Analyze(IssueCategory.ProjectSetting, i => i.Id.Equals("PAS0033"));
            var playerSettingIssue = issues.FirstOrDefault();

            Assert.NotNull(playerSettingIssue);

            AudioSettings.Reset(audioConfiguration);

            m_Platform = oldPlatform;
        }

        [TestCase(false)]
        [TestCase(true)]
        public void SettingsAnalysis_AudioMode_IsSpeakerModeMono(bool isSpeakerMonoMode)
        {
            var audioSpeaker = AudioSettings.GetConfiguration();
            AudioSettings.speakerMode =
                isSpeakerMonoMode ? AudioSpeakerMode.Mono : AudioSpeakerMode.Stereo;

            Assert.AreEqual(isSpeakerMonoMode, PlayerSettingsAnalyzer.IsSpeakerModeMono());

            AudioSettings.Reset(audioSpeaker);
        }

        [TestCase(AudioSpeakerMode.Mono)]
        [TestCase(AudioSpeakerMode.Stereo)]
        public void SettingsAnalysis_AudioMode_SwitchSpeakerMode(AudioSpeakerMode speakerMode)
        {
            var audioSpeaker = AudioSettings.GetConfiguration();
            AudioSettings.speakerMode = speakerMode;

            PlayerSettingsAnalyzer.FixSpeakerMode();

            Assert.AreEqual(AudioSpeakerMode.Mono, AudioSettings.speakerMode);

            AudioSettings.Reset(audioSpeaker);
        }

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

        [Test]
        [TestCase(Il2CppCompilerConfiguration.Debug)]
        [TestCase(Il2CppCompilerConfiguration.Master)]
        public void SettingsAnalysis_IL2CPP_Compiler_Configuration_IsReported(
            Il2CppCompilerConfiguration il2CppCompilerConfiguration)
        {
            var buildTargetGroup = BuildPipeline.GetBuildTargetGroup(m_Platform);
            var scriptingBackend = PlayerSettingsUtil.GetScriptingBackend(buildTargetGroup);

            PlayerSettingsUtil.SetScriptingBackend(buildTargetGroup, ScriptingImplementation.IL2CPP);

            var compilerConfiguration = PlayerSettingsUtil.GetIl2CppCompilerConfiguration(buildTargetGroup);
            PlayerSettingsUtil.SetIl2CppCompilerConfiguration(buildTargetGroup, il2CppCompilerConfiguration);

            var id = il2CppCompilerConfiguration == Il2CppCompilerConfiguration.Master
                ? PlayerSettingsAnalyzer.PAS1004
                : PlayerSettingsAnalyzer.PAS1005;

            var issues = Analyze(IssueCategory.ProjectSetting, i => i.Id.Equals(id));

            Assert.AreEqual(1, issues.Length);

            PlayerSettingsUtil.SetScriptingBackend(buildTargetGroup, scriptingBackend);
            PlayerSettingsUtil.SetIl2CppCompilerConfiguration(buildTargetGroup, compilerConfiguration);
        }

        [Test]
        [TestCase(PlayerSettingsAnalyzer.PAS1004)]
        [TestCase(PlayerSettingsAnalyzer.PAS1005)]
        public void SettingsAnalysis_Il2CppCompilerConfigurationRelease_IsNotReported(string id)
        {
            var buildTargetGroup = BuildPipeline.GetBuildTargetGroup(m_Platform);
            var scriptingBackend = PlayerSettingsUtil.GetScriptingBackend(buildTargetGroup);

            PlayerSettingsUtil.SetScriptingBackend(buildTargetGroup, ScriptingImplementation.IL2CPP);

            var compilerConfiguration = PlayerSettingsUtil.GetIl2CppCompilerConfiguration(buildTargetGroup);
            PlayerSettingsUtil.SetIl2CppCompilerConfiguration(buildTargetGroup, Il2CppCompilerConfiguration.Release);

            var issues = Analyze(IssueCategory.ProjectSetting, i => i.Id.Equals(id));

            Assert.AreEqual(0, issues.Length);

            PlayerSettingsUtil.SetScriptingBackend(buildTargetGroup, scriptingBackend);
            PlayerSettingsUtil.SetIl2CppCompilerConfiguration(buildTargetGroup, compilerConfiguration);
        }

        [Test]
        [TestCase(PlayerSettingsAnalyzer.PAS1004)]
        [TestCase(PlayerSettingsAnalyzer.PAS1005)]
        public void SettingsAnalysis_Il2CppCompilerConfigurationMaster_ScriptingBackendMono_IsNotReported(string id)
        {
            var buildTargetGroup = BuildPipeline.GetBuildTargetGroup(m_Platform);
            var scriptingBackend = PlayerSettingsUtil.GetScriptingBackend(buildTargetGroup);

            PlayerSettingsUtil.SetScriptingBackend(buildTargetGroup, ScriptingImplementation.Mono2x);

            var issues = Analyze(IssueCategory.ProjectSetting, i => i.Id.Equals(id));

            Assert.AreEqual(0, issues.Length);

            PlayerSettingsUtil.SetScriptingBackend(buildTargetGroup, scriptingBackend);
        }

        [Test]
        public void SettingsAnalysis_SwitchIL2CPP_Compiler_Configuration_To_Release()
        {
            var buildTargetGroup = BuildPipeline.GetBuildTargetGroup(m_Platform);
            var scriptingBackend = PlayerSettingsUtil.GetScriptingBackend(buildTargetGroup);

            PlayerSettingsUtil.SetScriptingBackend(buildTargetGroup, ScriptingImplementation.IL2CPP);
            var compilerConfiguration = PlayerSettingsUtil.GetIl2CppCompilerConfiguration(buildTargetGroup);

            PlayerSettingsUtil.SetIl2CppCompilerConfiguration(buildTargetGroup, Il2CppCompilerConfiguration.Debug);

            PlayerSettingsAnalyzer.SetIL2CPPConfigurationToRelease(buildTargetGroup);
            Assert.AreEqual(Il2CppCompilerConfiguration.Release, PlayerSettingsUtil.GetIl2CppCompilerConfiguration(buildTargetGroup));

            PlayerSettingsUtil.SetScriptingBackend(buildTargetGroup, scriptingBackend);
            PlayerSettingsUtil.SetIl2CppCompilerConfiguration(buildTargetGroup, compilerConfiguration);
        }

        [Test]
        public void SettingsAnalysis_LightmapStreaming_Disabled_Reported()
        {
            var buildTargetGroup = BuildPipeline.GetBuildTargetGroup(m_Platform);
            var currentState = PlayerSettingsUtil.IsLightmapStreamingEnabled(buildTargetGroup);

            PlayerSettingsUtil.SetLightmapStreaming(buildTargetGroup, false);

            var id = PlayerSettingsAnalyzer.PAS1006;
            var issues = Analyze(IssueCategory.ProjectSetting, i => i.Id.Equals(id));

            Assert.AreEqual(1, issues.Length);

            PlayerSettingsUtil.SetLightmapStreaming(buildTargetGroup, currentState);
        }

        [Test]
        public void SettingsAnalysis_LightmapStreaming_Disabled_IsNotReported()
        {
            var buildTargetGroup = BuildPipeline.GetBuildTargetGroup(m_Platform);
            var currentState = PlayerSettingsUtil.IsLightmapStreamingEnabled(buildTargetGroup);

            PlayerSettingsUtil.SetLightmapStreaming(buildTargetGroup, true);

            var id = PlayerSettingsAnalyzer.PAS1006;
            var issues = Analyze(IssueCategory.ProjectSetting, i => i.Id.Equals(id));

            Assert.AreEqual(0, issues.Length);

            PlayerSettingsUtil.SetLightmapStreaming(buildTargetGroup, currentState);
        }

        [Test]
        [TestCase(true)]
        [TestCase(false)]
        public void SettingsAnalysis_Enable_LightMapStreaming(bool isEnabled)
        {
            var buildTargetGroup = BuildPipeline.GetBuildTargetGroup(m_Platform);
            var currentState = PlayerSettingsUtil.IsLightmapStreamingEnabled(buildTargetGroup);

            PlayerSettingsUtil.SetLightmapStreaming(buildTargetGroup, isEnabled);
            Assert.AreEqual(isEnabled, PlayerSettingsUtil.IsLightmapStreamingEnabled(buildTargetGroup));

            PlayerSettingsUtil.SetLightmapStreaming(buildTargetGroup, currentState);
        }
    }
}
