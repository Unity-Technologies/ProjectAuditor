using System;
using NUnit.Framework;
using Unity.ProjectAuditor.Editor;
using Unity.ProjectAuditor.Editor.AssemblyUtils;
using Unity.ProjectAuditor.TestUtils;
using UnityEditor;
using UnityEngine;

namespace Unity.ProjectAuditor.EditorTests
{
    class ProjectAuditorTests : TestFixtureBase
    {
        [Test]
        public void ProjectAuditor_IsInstantiated()
        {
            Activator.CreateInstance(typeof(Unity.ProjectAuditor.Editor.ProjectAuditor));
        }

        [Test]
        public void ProjectAuditor_Module_IsSupported()
        {
#if BUILD_REPORT_API_SUPPORT
            Assert.True(m_ProjectAuditor.IsModuleSupported(IssueCategory.BuildFile));
#else
            Assert.False(m_ProjectAuditor.IsModuleSupported(IssueCategory.BuildFile));
#endif
        }

        [Test]
        public void ProjectAuditor_Category_IsRegistered()
        {
            const string testCategoryName = "TestCategory";

            Assert.AreEqual("Unknown", Editor.ProjectAuditor.GetCategoryName((IssueCategory)999));

            var numCategories = Unity.ProjectAuditor.Editor.ProjectAuditor.NumCategories();
            var category = Unity.ProjectAuditor.Editor.ProjectAuditor.GetOrRegisterCategory(testCategoryName);

            // check category is registered
            Assert.True(category >= IssueCategory.FirstCustomCategory);

            Assert.AreEqual(testCategoryName, Editor.ProjectAuditor.GetCategoryName(category));

            // check num category increased by 1
            Assert.AreEqual(numCategories + 1, Unity.ProjectAuditor.Editor.ProjectAuditor.NumCategories());

            // check category is still the same
            Assert.AreEqual(category,
                Unity.ProjectAuditor.Editor.ProjectAuditor.GetOrRegisterCategory(testCategoryName));
        }

        [Test]
        public void ProjectAuditor_Params_DefaultsAreCorrect()
        {
            var projectAuditorParams = new ProjectAuditorParams();

            Assert.IsNull(projectAuditorParams.categories);
            Assert.IsNull(projectAuditorParams.assemblyNames);
            Assert.AreEqual(EditorUserBuildSettings.activeBuildTarget, projectAuditorParams.platform);
            Assert.AreEqual(CodeOptimization.Release, projectAuditorParams.codeOptimization);
        }

        [Test]
        public void ProjectAuditor_Params_AreCopied()
        {
            var settingsProvider = new ProjectAuditorSettingsProvider();
            var settings = settingsProvider.GetCurrentSettings();

            var originalParams = new ProjectAuditorParams
            {
                categories = new[] { IssueCategory.Code },
                assemblyNames = new[] { "Test" },
                platform = BuildTarget.Android,
                codeOptimization = CodeOptimization.Debug,
                settings = settings
            };

            var projectAuditorParams = new ProjectAuditorParams(originalParams);

            Assert.IsNotNull(projectAuditorParams.categories);
            Assert.IsNotNull(projectAuditorParams.assemblyNames);
            Assert.AreEqual(BuildTarget.Android, projectAuditorParams.platform);
            Assert.AreEqual(CodeOptimization.Debug, projectAuditorParams.codeOptimization);
            Assert.AreEqual(settings, projectAuditorParams.settings);
        }

        [Test]
        public void ProjectAuditor_Params_CallbacksAreInvoked()
        {
            var config = ScriptableObject.CreateInstance<ProjectAuditorConfig>();
            config.CompilationMode = CompilationMode.Player;

            var projectAuditor = new Unity.ProjectAuditor.Editor.ProjectAuditor(config);

            int numModules = 0;
            ProjectReport projectReport = null;

            var settingsProvider = new ProjectAuditorSettingsProvider();
            settingsProvider.Initialize();

            projectAuditor.Audit(new ProjectAuditorParams
            {
                categories = new[] { IssueCategory.ProjectSetting },
                onModuleCompleted = () => numModules++,
                onCompleted = report =>
                {
                    Assert.Null(projectReport);
                    Assert.NotNull(report);

                    projectReport = report;
                },
                settings = settingsProvider.GetCurrentSettings()
            });

            Assert.AreEqual(1, numModules);
            Assert.NotNull(projectReport);
        }

        [Test]
        public void ProjectAuditor_Report_IsUpdated()
        {
            var savedSetting = PlayerSettings.bakeCollisionMeshes;
            PlayerSettings.bakeCollisionMeshes = false;

            var settingsProvider = new ProjectAuditorSettingsProvider();
            settingsProvider.Initialize();

            var projectAuditor = new Unity.ProjectAuditor.Editor.ProjectAuditor(m_Config);
            var report = projectAuditor.Audit(new ProjectAuditorParams
            {
                categories = new[] { IssueCategory.ProjectSetting},
                settings = settingsProvider.GetCurrentSettings()
            });

            Assert.True(report.HasCategory(IssueCategory.ProjectSetting));
            Assert.Positive(report.FindByCategory(IssueCategory.ProjectSetting).Count);

            report.ClearIssues(IssueCategory.ProjectSetting);

            Assert.False(report.HasCategory(IssueCategory.ProjectSetting));
            Assert.Zero(report.FindByCategory(IssueCategory.ProjectSetting).Count);

            projectAuditor.Audit(new ProjectAuditorParams
            {
                categories = new[] { IssueCategory.ProjectSetting},
                existingReport = report,
                settings = settingsProvider.GetCurrentSettings()
            });

            Assert.True(report.HasCategory(IssueCategory.ProjectSetting));
            Assert.Positive(report.FindByCategory(IssueCategory.ProjectSetting).Count);

            PlayerSettings.bakeCollisionMeshes = savedSetting;
        }
    }
}
