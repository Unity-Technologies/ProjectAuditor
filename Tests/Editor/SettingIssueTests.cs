using System;
using System.Linq;
using NUnit.Framework;
using Unity.ProjectAuditor.Editor;
using Unity.ProjectAuditor.Editor.SettingsAnalyzers;
using UnityEngine;

namespace UnityEditor.ProjectAuditor.EditorTests
{
    class SettingIssueTests
    {
        [Test]
        public void SettingIssuesIsReported()
        {
            var savedSetting = PlayerSettings.bakeCollisionMeshes;
            PlayerSettings.bakeCollisionMeshes = false;

            var projectAuditor = new Unity.ProjectAuditor.Editor.ProjectAuditor();
            var projectReport = projectAuditor.Audit();
            var issues = projectReport.GetIssues(IssueCategory.ProjectSettings);

            var playerSettingIssue =
                issues.FirstOrDefault(i => i.descriptor.method.Equals("Player: Prebake Collision Meshes"));
            Assert.NotNull(playerSettingIssue);
            Assert.True(playerSettingIssue.location.Path.Equals("Project/Player"));

            PlayerSettings.bakeCollisionMeshes = savedSetting;
        }

        [Test]
        public void SettingIssuesIsNotReportedOnceFixed()
        {
            var savedFixedDeltaTime = Time.fixedDeltaTime;
            // 0.02f is the default Time.fixedDeltaTime value and will be reported as an issue
            Time.fixedDeltaTime = 0.02f;

            var projectAuditor = new Unity.ProjectAuditor.Editor.ProjectAuditor();
            var projectReport = projectAuditor.Audit();
            var issues = projectReport.GetIssues(IssueCategory.ProjectSettings);
            var fixedDeltaTimeIssue = issues.FirstOrDefault(i => i.descriptor.method.Equals("fixedDeltaTime"));
            Assert.NotNull(fixedDeltaTimeIssue);
            Assert.True(fixedDeltaTimeIssue.description.Equals("Time: Fixed Timestep"));
            Assert.True(fixedDeltaTimeIssue.location.Path.Equals("Project/Time"));

            // "fix" fixedDeltaTime so it's not reported anymore
            Time.fixedDeltaTime = 0.021f;

            projectReport = projectAuditor.Audit();
            issues = projectReport.GetIssues(IssueCategory.ProjectSettings);
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

            var evaluators = new Evaluators();
            Assert.AreEqual(splashScreenEnabled, evaluators.PlayerSettingsSplashScreenIsEnabledAndCanBeDisabled());

            PlayerSettings.SplashScreen.show = prevSplashScreenEnabled;
        }
    }
}
