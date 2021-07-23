using System;
using System.Linq;
using NUnit.Framework;
using Unity.ProjectAuditor.Editor;
using Unity.ProjectAuditor.Editor.Auditors;
using Unity.ProjectAuditor.Editor.SettingsAnalyzers;
using Unity.ProjectAuditor.Editor.Utils;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEditor.ProjectAuditor.EditorTests
{
    class SettingIssueTests
    {
        [Test]
        public void SettingIssuesIsReported()
        {
            var savedSetting = PlayerSettings.bakeCollisionMeshes;
            PlayerSettings.bakeCollisionMeshes = false;

            var issues = Utility.Analyze(IssueCategory.ProjectSetting);
            var playerSettingIssue = issues.FirstOrDefault(i => i.descriptor.method.Equals("bakeCollisionMeshes"));

            Assert.NotNull(playerSettingIssue);
            Assert.True(playerSettingIssue.description.Equals("Player: Prebake Collision Meshes"));
            Assert.True(playerSettingIssue.location.Path.Equals("Project/Player"));
            Assert.AreEqual(2, playerSettingIssue.descriptor.GetAreas().Length);
            Assert.Contains(Area.BuildSize, playerSettingIssue.descriptor.GetAreas());
            Assert.Contains(Area.LoadTime, playerSettingIssue.descriptor.GetAreas());

            // restore bakeCollisionMeshes
            PlayerSettings.bakeCollisionMeshes = savedSetting;
        }

        [Test]
        public void SettingIssuesIsNotReportedOnceFixed()
        {
            var savedFixedDeltaTime = Time.fixedDeltaTime;
            // 0.02f is the default Time.fixedDeltaTime value and will be reported as an issue
            Time.fixedDeltaTime = 0.02f;

            var issues = Utility.Analyze(IssueCategory.ProjectSetting);
            var fixedDeltaTimeIssue = issues.FirstOrDefault(i => i.descriptor.method.Equals("fixedDeltaTime"));
            Assert.NotNull(fixedDeltaTimeIssue);
            Assert.True(fixedDeltaTimeIssue.description.Equals("Time: Fixed Timestep"));
            Assert.True(fixedDeltaTimeIssue.location.Path.Equals("Project/Time"));

            // "fix" fixedDeltaTime so it's not reported anymore
            Time.fixedDeltaTime = 0.021f;

            issues = Utility.Analyze(IssueCategory.ProjectSetting);
            Assert.Null(issues.FirstOrDefault(i => i.descriptor.method.Equals("fixedDeltaTime")));

            // restore Time.fixedDeltaTime
            Time.fixedDeltaTime = savedFixedDeltaTime;
        }

        [Test]
        [TestCase(false)]
        [TestCase(true)]
        public void  SplashScreenIsEnabledAndCanBeDisabled(bool splashScreenEnabled)
        {
            var prevSplashScreenEnabled = PlayerSettings.SplashScreen.show;
            PlayerSettings.SplashScreen.show = splashScreenEnabled;

            Assert.AreEqual(splashScreenEnabled, Evaluators.PlayerSettingsSplashScreenIsEnabledAndCanBeDisabled());

            PlayerSettings.SplashScreen.show = prevSplashScreenEnabled;
        }

        [TestCase(false)]
        [TestCase(true)]
        public void GraphicsMixedStandardShaderQualityIsReported(bool isMixed)
        {
            var buildGroup = BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget);
            var savedTier1settings = EditorGraphicsSettings.GetTierSettings(buildGroup, GraphicsTier.Tier1);
            var savedTier2settings = EditorGraphicsSettings.GetTierSettings(buildGroup, GraphicsTier.Tier2);
            var savedTier3settings = EditorGraphicsSettings.GetTierSettings(buildGroup, GraphicsTier.Tier3);

            var tier1settings = EditorGraphicsSettings.GetTierSettings(buildGroup, GraphicsTier.Tier1);
            var tier2settings = EditorGraphicsSettings.GetTierSettings(buildGroup, GraphicsTier.Tier2);
            var tier3settings = EditorGraphicsSettings.GetTierSettings(buildGroup, GraphicsTier.Tier3);

            tier1settings.standardShaderQuality = ShaderQuality.High;
            tier2settings.standardShaderQuality = ShaderQuality.High;
            tier3settings.standardShaderQuality = isMixed ? ShaderQuality.Low : ShaderQuality.High;

            EditorGraphicsSettings.SetTierSettings(buildGroup, GraphicsTier.Tier1, tier1settings);
            EditorGraphicsSettings.SetTierSettings(buildGroup, GraphicsTier.Tier2, tier2settings);
            EditorGraphicsSettings.SetTierSettings(buildGroup, GraphicsTier.Tier3, tier3settings);

            Assert.AreEqual(isMixed, Evaluators.GraphicsMixedStandardShaderQuality());

            EditorGraphicsSettings.SetTierSettings(buildGroup, GraphicsTier.Tier1, savedTier1settings);
            EditorGraphicsSettings.SetTierSettings(buildGroup, GraphicsTier.Tier2, savedTier2settings);
            EditorGraphicsSettings.SetTierSettings(buildGroup, GraphicsTier.Tier3, savedTier3settings);
        }

        [TestCase(RenderingPath.Forward)]
        [TestCase(RenderingPath.DeferredShading)]
        public void GraphicsUsingRenderingPathIsReported(RenderingPath renderingPath)
        {
            var buildGroup = BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget);
            var savedTier1settings = EditorGraphicsSettings.GetTierSettings(buildGroup, GraphicsTier.Tier1);
            var savedTier2settings = EditorGraphicsSettings.GetTierSettings(buildGroup, GraphicsTier.Tier2);
            var savedTier3settings = EditorGraphicsSettings.GetTierSettings(buildGroup, GraphicsTier.Tier3);

            var tier1settings = EditorGraphicsSettings.GetTierSettings(buildGroup, GraphicsTier.Tier1);
            var tier2settings = EditorGraphicsSettings.GetTierSettings(buildGroup, GraphicsTier.Tier2);
            var tier3settings = EditorGraphicsSettings.GetTierSettings(buildGroup, GraphicsTier.Tier3);

            tier1settings.renderingPath = renderingPath;
            tier2settings.renderingPath = renderingPath;
            tier3settings.renderingPath = renderingPath;

            EditorGraphicsSettings.SetTierSettings(buildGroup, GraphicsTier.Tier1, tier1settings);
            EditorGraphicsSettings.SetTierSettings(buildGroup, GraphicsTier.Tier2, tier2settings);
            EditorGraphicsSettings.SetTierSettings(buildGroup, GraphicsTier.Tier3, tier3settings);

            if (renderingPath == RenderingPath.Forward)
            {
                Assert.AreEqual(true, Evaluators.GraphicsUsingForwardRendering());
                Assert.AreEqual(false, Evaluators.GraphicsUsingDeferredRendering());
            }
            else
            {
                Assert.AreEqual(false, Evaluators.GraphicsUsingForwardRendering());
                Assert.AreEqual(true, Evaluators.GraphicsUsingDeferredRendering());
            }

            EditorGraphicsSettings.SetTierSettings(buildGroup, GraphicsTier.Tier1, savedTier1settings);
            EditorGraphicsSettings.SetTierSettings(buildGroup, GraphicsTier.Tier2, savedTier2settings);
            EditorGraphicsSettings.SetTierSettings(buildGroup, GraphicsTier.Tier3, savedTier3settings);
        }
    }
}
