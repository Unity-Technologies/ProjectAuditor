using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using Unity.ProjectAuditor.Editor;
using Unity.ProjectAuditor.Editor.Core;
using Unity.ProjectAuditor.Editor.Diagnostic;
using Unity.ProjectAuditor.Editor.Modules;
using Unity.ProjectAuditor.Editor.SettingsAnalysis;
using Unity.ProjectAuditor.Editor.Tests.Common;
using Unity.ProjectAuditor.Editor.Utils;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;
using FogMode = Unity.ProjectAuditor.Editor.Modules.FogMode;

namespace Unity.ProjectAuditor.EditorTests
{
    class SettingsAnalysisTests : TestFixtureBase
    {
        [Test]
        public void SettingsAnalysis_Default_AccelerometerFrequency()
        {
            Assert.True(PlayerSettingsAnalyzer.IsAccelerometerEnabled());
        }

        [Test]
        public void SettingsAnalysis_Default_PhysicsLayerCollisionMatrix()
        {
            Assert.True(PhysicsAnalyzer.IsDefaultLayerCollisionMatrix());
        }

        [Test]
        public void SettingsAnalysis_Default_Physics2DLayerCollisionMatrix()
        {
            Assert.True(Physics2DAnalyzer.IsDefaultLayerCollisionMatrix());
        }

        [Test]
        public void SettingsAnalysis_Default_QualitySettings()
        {
            Assert.True(QualitySettingsAnalyzer.IsUsingDefaultSettings());
        }

        [Test]
        public void SettingsAnalysis_Default_QualityAsyncUploadTimeSlice()
        {
            Assert.True(QualitySettingsAnalyzer.IsDefaultAsyncUploadTimeSlice());
        }

        [Test]
        public void SettingsAnalysis_Default_QualityAsyncUploadBufferSize()
        {
            Assert.True(QualitySettingsAnalyzer.IsDefaultAsyncUploadBufferSize());
        }

        [Test]
        public void SettingsAnalysis_Default_StaticBatchingEnabled()
        {
            Assert.True(PlayerSettingsUtil.IsStaticBatchingEnabled(EditorUserBuildSettings.activeBuildTarget));
        }

        [Test]
        public void SettingsAnalysis_Issue_IsReported()
        {
            var savedSetting = PlayerSettings.bakeCollisionMeshes;
            PlayerSettings.bakeCollisionMeshes = false;

            var issues = Analyze(IssueCategory.ProjectSetting, i => i.descriptor.method.Equals("bakeCollisionMeshes"));
            var playerSettingIssue = issues.FirstOrDefault();

            Assert.NotNull(playerSettingIssue);
            Assert.AreEqual("Player: Prebake Collision Meshes", playerSettingIssue.description);
            Assert.AreEqual("Project/Player", playerSettingIssue.location.Path);
            Assert.AreEqual("Player", playerSettingIssue.location.Filename);
            Assert.AreEqual(2, playerSettingIssue.descriptor.GetAreas().Length);
            Assert.Contains(Area.BuildSize, playerSettingIssue.descriptor.GetAreas());
            Assert.Contains(Area.LoadTime, playerSettingIssue.descriptor.GetAreas());
            Assert.AreEqual("Any", playerSettingIssue.descriptor.GetPlatformsSummary());

            // restore bakeCollisionMeshes
            PlayerSettings.bakeCollisionMeshes = savedSetting;
        }

        [Test]
        public void SettingsAnalysis_Issue_IsNotReportedOnceFixed()
        {
            var savedFixedDeltaTime = Time.fixedDeltaTime;
            // 0.02f is the default Time.fixedDeltaTime value and will be reported as an issue
            Time.fixedDeltaTime = 0.02f;

            var issues = Analyze(IssueCategory.ProjectSetting, i => i.descriptor.title.Equals("Time: Fixed Timestep"));
            var fixedDeltaTimeIssue = issues.FirstOrDefault();
            Assert.NotNull(fixedDeltaTimeIssue);
            Assert.AreEqual("Time: Fixed Timestep", fixedDeltaTimeIssue.description);
            Assert.AreEqual("Project/Time", fixedDeltaTimeIssue.location.Path);

            // "fix" fixedDeltaTime so it's not reported anymore
            Time.fixedDeltaTime = 0.021f;

            issues = Analyze(IssueCategory.ProjectSetting, i => i.descriptor.title.Equals("Time: Fixed Timestep"));
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
        public void SettingsAnalysis_AudioMode_SpeakerModeStereo_IsReported()
        {
            var audioConfiguration = AudioSettings.GetConfiguration();
            AudioSettings.speakerMode = AudioSpeakerMode.Stereo;

            var issues = Analyze(IssueCategory.ProjectSetting, i => i.descriptor.id.Equals("PAS0033"));
            var playerSettingIssue = issues.FirstOrDefault();

            Assert.NotNull(playerSettingIssue);

            AudioSettings.Reset(audioConfiguration);
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
            var buildTarget = EditorUserBuildSettings.activeBuildTarget;
            var buildGroup = BuildPipeline.GetBuildTargetGroup(buildTarget);
            var savedTier1settings = EditorGraphicsSettings.GetTierSettings(buildGroup, GraphicsTier.Tier1);
            var savedTier2settings = EditorGraphicsSettings.GetTierSettings(buildGroup, GraphicsTier.Tier2);
            var savedTier3settings = EditorGraphicsSettings.GetTierSettings(buildGroup, GraphicsTier.Tier3);

            var tier1settings = EditorGraphicsSettings.GetTierSettings(buildGroup, GraphicsTier.Tier1);
            var tier2settings = EditorGraphicsSettings.GetTierSettings(buildGroup, GraphicsTier.Tier2);
            var tier3settings = EditorGraphicsSettings.GetTierSettings(buildGroup, GraphicsTier.Tier3);
#if UNITY_2019_3_OR_NEWER
            var defaultRenderPipeline = GraphicsSettings.defaultRenderPipeline;
#endif
            tier1settings.standardShaderQuality = ShaderQuality.High;
            tier2settings.standardShaderQuality = ShaderQuality.High;
            tier3settings.standardShaderQuality = isMixed ? ShaderQuality.Low : ShaderQuality.High;

            EditorGraphicsSettings.SetTierSettings(buildGroup, GraphicsTier.Tier1, tier1settings);
            EditorGraphicsSettings.SetTierSettings(buildGroup, GraphicsTier.Tier2, tier2settings);
            EditorGraphicsSettings.SetTierSettings(buildGroup, GraphicsTier.Tier3, tier3settings);
#if UNITY_2019_3_OR_NEWER
            GraphicsSettings.defaultRenderPipeline = null;
#endif
            Assert.AreEqual(isMixed, BuiltinRenderPipelineAnalyzer.IsMixedStandardShaderQuality(buildTarget));

            EditorGraphicsSettings.SetTierSettings(buildGroup, GraphicsTier.Tier1, savedTier1settings);
            EditorGraphicsSettings.SetTierSettings(buildGroup, GraphicsTier.Tier2, savedTier2settings);
            EditorGraphicsSettings.SetTierSettings(buildGroup, GraphicsTier.Tier3, savedTier3settings);
#if UNITY_2019_3_OR_NEWER
            GraphicsSettings.defaultRenderPipeline = defaultRenderPipeline;
#endif
        }

        [TestCase(RenderingPath.Forward)]
        [TestCase(RenderingPath.DeferredShading)]
        public void SettingsAnalysis_GraphicsUsingRenderingPath_WithBuiltinRenderPipeline_IsReported(RenderingPath renderingPath)
        {
            var buildTarget = EditorUserBuildSettings.activeBuildTarget;
            var buildGroup = BuildPipeline.GetBuildTargetGroup(buildTarget);
            var savedTier1settings = EditorGraphicsSettings.GetTierSettings(buildGroup, GraphicsTier.Tier1);
            var savedTier2settings = EditorGraphicsSettings.GetTierSettings(buildGroup, GraphicsTier.Tier2);
            var savedTier3settings = EditorGraphicsSettings.GetTierSettings(buildGroup, GraphicsTier.Tier3);

            var tier1settings = EditorGraphicsSettings.GetTierSettings(buildGroup, GraphicsTier.Tier1);
            var tier2settings = EditorGraphicsSettings.GetTierSettings(buildGroup, GraphicsTier.Tier2);
            var tier3settings = EditorGraphicsSettings.GetTierSettings(buildGroup, GraphicsTier.Tier3);
#if UNITY_2019_3_OR_NEWER
            var defaultRenderPipeline = GraphicsSettings.defaultRenderPipeline;
#endif
            tier1settings.renderingPath = renderingPath;
            tier2settings.renderingPath = renderingPath;
            tier3settings.renderingPath = renderingPath;

            EditorGraphicsSettings.SetTierSettings(buildGroup, GraphicsTier.Tier1, tier1settings);
            EditorGraphicsSettings.SetTierSettings(buildGroup, GraphicsTier.Tier2, tier2settings);
            EditorGraphicsSettings.SetTierSettings(buildGroup, GraphicsTier.Tier3, tier3settings);
#if UNITY_2019_3_OR_NEWER
            GraphicsSettings.defaultRenderPipeline = null;
#endif
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

            EditorGraphicsSettings.SetTierSettings(buildGroup, GraphicsTier.Tier1, savedTier1settings);
            EditorGraphicsSettings.SetTierSettings(buildGroup, GraphicsTier.Tier2, savedTier2settings);
            EditorGraphicsSettings.SetTierSettings(buildGroup, GraphicsTier.Tier3, savedTier3settings);
#if UNITY_2019_3_OR_NEWER
            GraphicsSettings.defaultRenderPipeline = defaultRenderPipeline;
#endif
        }

        [Test]
        [TestCase(FogMode.Exponential)]
        [TestCase(FogMode.ExponentialSquared)]
        [TestCase(FogMode.Linear)]
        public void SettingsAnalysis_FogStripping_IsReported(FogMode fogMode)
        {
            var graphicsSettings = GraphicsSettingsProxy.GetGraphicsSettings();
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

            var issues = Analyze(IssueCategory.ProjectSetting, i => i.descriptor.id.Equals(FogStrippingAnalyzer.PAS1003));

            Assert.AreEqual(1, issues.Length);
            string description = $"Graphics: FogMode '{fogMode}' shader variants is always included in the build.";
            Assert.AreEqual(description, issues[0].description);

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
            var graphicsSettings = GraphicsSettingsProxy.GetGraphicsSettings();
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

            var issues = Analyze(IssueCategory.ProjectSetting, i => i.descriptor.id.Equals(FogStrippingAnalyzer.PAS1003));
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
            var buildTargetGroup = EditorUserBuildSettings.selectedBuildTargetGroup;
            var settings = PlayerSettings.GetScriptingBackend(buildTargetGroup);

            PlayerSettings.SetScriptingBackend(buildTargetGroup, ScriptingImplementation.IL2CPP);

            var compilerConfiguration = PlayerSettings.GetIl2CppCompilerConfiguration(buildTargetGroup);
            PlayerSettings.SetIl2CppCompilerConfiguration(buildTargetGroup, il2CppCompilerConfiguration);

            ProjectIssue[] issues = null;

            var id = il2CppCompilerConfiguration == Il2CppCompilerConfiguration.Master
                ? PlayerSettingsAnalyzer.PAS1004
                : PlayerSettingsAnalyzer.PAS1005;

            issues = Analyze(IssueCategory.ProjectSetting, i => i.descriptor.id.Equals(id));

            var playerSettingIssue = issues.Length;

            Assert.AreEqual(1, playerSettingIssue);

            PlayerSettings.SetScriptingBackend(buildTargetGroup, settings);
            PlayerSettings.SetIl2CppCompilerConfiguration(buildTargetGroup, compilerConfiguration);
        }

        [Test]
        [TestCase(PlayerSettingsAnalyzer.PAS1004)]
        [TestCase(PlayerSettingsAnalyzer.PAS1005)]
        public void SettingsAnalysis_Il2CppCompilerConfigurationRelease_IsNotReported(string id)
        {
            var buildTargetGroup = EditorUserBuildSettings.selectedBuildTargetGroup;
            var settings = PlayerSettings.GetScriptingBackend(buildTargetGroup);

            PlayerSettings.SetScriptingBackend(buildTargetGroup, ScriptingImplementation.IL2CPP);

            var compilerConfiguration = PlayerSettings.GetIl2CppCompilerConfiguration(buildTargetGroup);
            PlayerSettings.SetIl2CppCompilerConfiguration(buildTargetGroup, Il2CppCompilerConfiguration.Release);

            ProjectIssue[] issues = null;

            issues = Analyze(IssueCategory.ProjectSetting, i => i.descriptor.id.Equals(id));
            var playerSettingIssue = issues.FirstOrDefault();

            Assert.IsNull(playerSettingIssue);

            PlayerSettings.SetScriptingBackend(buildTargetGroup, settings);
            PlayerSettings.SetIl2CppCompilerConfiguration(buildTargetGroup, compilerConfiguration);
        }

        [Test]
        [TestCase(PlayerSettingsAnalyzer.PAS1004)]
        [TestCase(PlayerSettingsAnalyzer.PAS1005)]
        public void SettingsAnalysis_Il2CppCompilerConfigurationMaster_ScriptingBackendMono_IsNotReported(string id)
        {
            var buildTargetGroup = EditorUserBuildSettings.selectedBuildTargetGroup;
            var settings = PlayerSettings.GetScriptingBackend(buildTargetGroup);

            PlayerSettings.SetScriptingBackend(buildTargetGroup, ScriptingImplementation.Mono2x);

            ProjectIssue[] issues = null;

            issues = Analyze(IssueCategory.ProjectSetting, i => i.descriptor.id.Equals(id));
            var playerSettingIssue = issues.FirstOrDefault();

            Assert.IsNull(playerSettingIssue);

            PlayerSettings.SetScriptingBackend(buildTargetGroup, settings);
        }

        [Test]
        public void SettingsAnalysis_SwitchIL2CPP_Compiler_Configuration_To_Release()
        {
            var buildTargetGroup = EditorUserBuildSettings.selectedBuildTargetGroup;
            var settings = PlayerSettings.GetScriptingBackend(buildTargetGroup);

            PlayerSettings.SetScriptingBackend(buildTargetGroup, ScriptingImplementation.IL2CPP);
            var compilerConfiguration = PlayerSettings.GetIl2CppCompilerConfiguration(buildTargetGroup);

            PlayerSettings.SetIl2CppCompilerConfiguration(buildTargetGroup, Il2CppCompilerConfiguration.Debug);

            PlayerSettingsAnalyzer.SetIL2CPPConfigurationToRelease();
            Assert.AreEqual(Il2CppCompilerConfiguration.Release, PlayerSettings.GetIl2CppCompilerConfiguration(buildTargetGroup));

            PlayerSettings.SetScriptingBackend(buildTargetGroup, settings);
            PlayerSettings.SetIl2CppCompilerConfiguration(buildTargetGroup, compilerConfiguration);
        }

        [Test]
        public void SettingsAnalysis_LightmapStreaming_Disabled_Reported()
        {
            var buildTargetGroup = EditorUserBuildSettings.selectedBuildTargetGroup;
            var currentState = PlayerSettingsUtil.IsLightmapStreamingEnabled(buildTargetGroup);

            PlayerSettingsUtil.SetLightmapStreaming(buildTargetGroup, false);

            var id = PlayerSettingsAnalyzer.PAS1006;
            var issues = Analyze(IssueCategory.ProjectSetting, i => i.descriptor.id.Equals(id));
            var playerSettingIssue = issues.FirstOrDefault();

            Assert.NotNull(playerSettingIssue);

            PlayerSettingsUtil.SetLightmapStreaming(buildTargetGroup, currentState);
        }

        [Test]
        public void SettingsAnalysis_LightmapStreaming_Disabled_Is_Not_Reported()
        {
            var buildTargetGroup = EditorUserBuildSettings.selectedBuildTargetGroup;
            var currentState = PlayerSettingsUtil.IsLightmapStreamingEnabled(buildTargetGroup);

            PlayerSettingsUtil.SetLightmapStreaming(buildTargetGroup, true);

            var id = PlayerSettingsAnalyzer.PAS1006;
            var issues = Analyze(IssueCategory.ProjectSetting, i => i.descriptor.id.Equals(id));
            var playerSettingIssue = issues.FirstOrDefault();

            Assert.IsNull(playerSettingIssue);

            PlayerSettingsUtil.SetLightmapStreaming(buildTargetGroup, currentState);
        }

        [Test]
        [TestCase(true)]
        [TestCase(false)]
        public void SettingsAnalysis_Enable_LightMapStreaming(bool isEnabled)
        {
            var buildTargetGroup = EditorUserBuildSettings.selectedBuildTargetGroup;
            var currentState = PlayerSettingsUtil.IsLightmapStreamingEnabled(buildTargetGroup);

            PlayerSettingsUtil.SetLightmapStreaming(buildTargetGroup, isEnabled);
            Assert.AreEqual(isEnabled, PlayerSettingsUtil.IsLightmapStreamingEnabled(buildTargetGroup));

            PlayerSettingsUtil.SetLightmapStreaming(buildTargetGroup, currentState);
        }

        [Test]
        public void SettingsAnalysis_MipmapStreaming_Disabled_Reported()
        {
            int initialQualityLevel = QualitySettings.GetQualityLevel();
            List<bool> qualityLevelsValues = new List<bool>();

            for (var i = 0; i < QualitySettings.names.Length; i++)
            {
                QualitySettings.SetQualityLevel(i);
                qualityLevelsValues.Add(QualitySettings.streamingMipmapsActive);
                QualitySettings.streamingMipmapsActive = false;

                var id = QualitySettingsAnalyzer.PAS1007;
                var issues = Analyze(IssueCategory.ProjectSetting, j => j.descriptor.id.Equals(id));
                var qualitySettingIssue = issues.FirstOrDefault();

                Assert.NotNull(qualitySettingIssue);
            }

            ResetQualityLevelsValues(qualityLevelsValues);
            QualitySettings.SetQualityLevel(initialQualityLevel);
        }

        [Test]
        public void SettingsAnalysis_MipmapStreaming_Enabled_Is_Not_Reported()
        {
            int initialQualityLevel = QualitySettings.GetQualityLevel();
            List<bool> qualityLevelsValues = new List<bool>();

            for (var i = 0; i < QualitySettings.names.Length; i++)
            {
                QualitySettings.SetQualityLevel(i);
                qualityLevelsValues.Add(QualitySettings.streamingMipmapsActive);

                QualitySettings.streamingMipmapsActive = true;
            }

            var id = QualitySettingsAnalyzer.PAS1007;
            var issues = Analyze(IssueCategory.ProjectSetting, j => j.descriptor.id.Equals(id));
            var qualitySettingIssue = issues.FirstOrDefault();

            Assert.IsNull(qualitySettingIssue);

            ResetQualityLevelsValues(qualityLevelsValues);
            QualitySettings.SetQualityLevel(initialQualityLevel);
        }

        [Test]
        public void SettingsAnalysis_Enable_StreamingMipmap()
        {
            int initialQualityLevel = QualitySettings.GetQualityLevel();
            List<bool> qualityLevelsValues = new List<bool>();

            for (var i = 0; i < QualitySettings.names.Length; i++)
            {
                QualitySettings.SetQualityLevel(i);
                qualityLevelsValues.Add(QualitySettings.streamingMipmapsActive);
                QualitySettings.streamingMipmapsActive = false;

                QualitySettingsAnalyzer.EnableStreamingMipmap(i);
                Assert.IsTrue(QualitySettings.streamingMipmapsActive);
            }

            ResetQualityLevelsValues(qualityLevelsValues);
            QualitySettings.SetQualityLevel(initialQualityLevel);
        }

        void ResetQualityLevelsValues(List<bool> values)
        {
            for (var i = 0; i < QualitySettings.names.Length; i++)
            {
                QualitySettings.SetQualityLevel(i);
                QualitySettings.streamingMipmapsActive = values[i];
            }
        }
    }
}
