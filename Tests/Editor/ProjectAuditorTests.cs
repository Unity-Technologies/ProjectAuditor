using System;
using NUnit.Framework;
using Unity.ProjectAuditor.Editor;
using Unity.ProjectAuditor.Editor.AssemblyUtils;
using Unity.ProjectAuditor.Editor.Tests.Common;
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

            Assert.IsNull(projectAuditorParams.Categories);
            Assert.IsNull(projectAuditorParams.AssemblyNames);
            // projectAuditorParams.Platform defaults to NoTarget because we can't call EditorUserBuildSettings.activeBuildTarget
            // during construction/serialization. Platform gets set when params is passed to an instance of ProjectAuditor
            Assert.AreEqual(BuildTarget.NoTarget, projectAuditorParams.Platform);
            Assert.AreEqual(CodeOptimization.Release, projectAuditorParams.CodeOptimization);
        }

        [Test]
        public void ProjectAuditor_Params_AreCopied()
        {
            var rules = new ProjectAuditorRules();

            var originalParams = new ProjectAuditorParams
            {
                Categories = new[] { IssueCategory.Code },
                AssemblyNames = new[] { "Test" },
                Platform = BuildTarget.Android,
                CodeOptimization = CodeOptimization.Debug,
                Rules = rules
            };

            var projectAuditorParams = new ProjectAuditorParams(originalParams);

            Assert.IsNotNull(projectAuditorParams.Categories);
            Assert.IsNotNull(projectAuditorParams.AssemblyNames);
            Assert.AreEqual(BuildTarget.Android, projectAuditorParams.Platform);
            Assert.AreEqual(CodeOptimization.Debug, projectAuditorParams.CodeOptimization);
            Assert.AreEqual(rules, projectAuditorParams.Rules);
        }

        [Test]
        public void ProjectAuditor_Params_CallbacksAreInvoked()
        {
            var projectAuditor = new Unity.ProjectAuditor.Editor.ProjectAuditor();

            int numModules = 0;
            ProjectReport projectReport = null;

            projectAuditor.Audit(new ProjectAuditorParams
            {
                Categories = new[] { IssueCategory.ProjectSetting },
                OnModuleCompleted = () => numModules++,
                OnCompleted = report =>
                {
                    Assert.Null(projectReport);
                    Assert.NotNull(report);

                    projectReport = report;
                },
                CompilationMode = CompilationMode.Player
            });

            Assert.AreEqual(1, numModules);
            Assert.NotNull(projectReport);
        }

        [Test]
        public void ProjectAuditor_Report_IsUpdated()
        {
            var savedSetting = PlayerSettings.bakeCollisionMeshes;
            PlayerSettings.bakeCollisionMeshes = false;

            var projectAuditor = new Unity.ProjectAuditor.Editor.ProjectAuditor();
            var report = projectAuditor.Audit(new ProjectAuditorParams
            {
                Categories = new[] { IssueCategory.ProjectSetting}
            });

            Assert.True(report.HasCategory(IssueCategory.ProjectSetting));
            Assert.Positive(report.FindByCategory(IssueCategory.ProjectSetting).Count);

            report.ClearIssues(IssueCategory.ProjectSetting);

            Assert.False(report.HasCategory(IssueCategory.ProjectSetting));
            Assert.Zero(report.FindByCategory(IssueCategory.ProjectSetting).Count);

            projectAuditor.Audit(new ProjectAuditorParams
            {
                Categories = new[] { IssueCategory.ProjectSetting},
                ExistingReport = report
            });

            Assert.True(report.HasCategory(IssueCategory.ProjectSetting));
            Assert.Positive(report.FindByCategory(IssueCategory.ProjectSetting).Count);

            PlayerSettings.bakeCollisionMeshes = savedSetting;
        }
    }
}
