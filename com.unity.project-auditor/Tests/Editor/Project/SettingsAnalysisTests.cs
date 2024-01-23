using System.Linq;
using NUnit.Framework;
using Unity.ProjectAuditor.Editor;
using Unity.ProjectAuditor.Editor.Core;
using Unity.ProjectAuditor.Editor.SettingsAnalysis;
using Unity.ProjectAuditor.Editor.Tests.Common;
using Unity.ProjectAuditor.Editor.Utils;
using UnityEditor;
using UnityEditor.TestTools;
using UnityEngine;

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

        [Test]
        public void SettingsAnalysis_Issue_IsReported()
        {
            var issues = Analyze(IssueCategory.ProjectSetting, i =>
                i.Id.IsValid() && i.Id.GetDescriptor().Method.Equals(nameof(PlayerSettings.bakeCollisionMeshes)));

            var playerSettingIssue = issues.FirstOrDefault();
            var descriptor = playerSettingIssue.Id.GetDescriptor();

            Assert.NotNull(playerSettingIssue, "Issue not found");
            Assert.AreEqual("Player: Prebake Collision Meshes is disabled", playerSettingIssue.Description);
            Assert.AreEqual("Project/Player", playerSettingIssue.Location.Path);
            Assert.AreEqual("Player", playerSettingIssue.Location.Filename);
            Assert.AreEqual((Areas.BuildSize | Areas.LoadTime), descriptor.Areas);
            Assert.AreEqual("Any", descriptor.GetPlatformsSummary());
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
