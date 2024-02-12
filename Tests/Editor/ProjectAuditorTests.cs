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
            Assert.True(m_ProjectAuditor.IsModuleSupported(IssueCategory.BuildFile));
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
            var analysisParams = new AnalysisParams();

            Assert.IsNull(analysisParams.Categories);
            Assert.IsNull(analysisParams.AssemblyNames);
            // analysisParams.Platform defaults to NoTarget because we can't call EditorUserBuildSettings.activeBuildTarget
            // during construction/serialization. Platform gets set when params is passed to an instance of ProjectAuditor
            Assert.AreEqual(BuildTarget.NoTarget, analysisParams.Platform);
            Assert.AreEqual(CodeOptimization.Release, analysisParams.CodeOptimization);
        }

        [Test]
        public void ProjectAuditor_Params_AreCopied()
        {
            var rules = new SeverityRules();

            var originalParams = new AnalysisParams
            {
                Categories = new[] { IssueCategory.Code },
                AssemblyNames = new[] { "Test" },
                Platform = BuildTarget.Android,
                CodeOptimization = CodeOptimization.Debug,
                Rules = rules
            };

            var analysisParams = new AnalysisParams(originalParams);

            Assert.IsNotNull(analysisParams.Categories);
            Assert.IsNotNull(analysisParams.AssemblyNames);
            Assert.AreEqual(BuildTarget.Android, analysisParams.Platform);
            Assert.AreEqual(CodeOptimization.Debug, analysisParams.CodeOptimization);
            Assert.AreEqual(rules, analysisParams.Rules);
        }

        [Test]
        public void ProjectAuditor_Params_CallbacksAreInvoked()
        {
            var projectAuditor = new Unity.ProjectAuditor.Editor.ProjectAuditor();

            int numModules = 0;
            Report report = null;

            projectAuditor.Audit(new AnalysisParams
            {
                Categories = new[] { IssueCategory.ProjectSetting },
                OnModuleCompleted = (analysisResult) => numModules++,
                OnCompleted = completedReport =>
                {
                    Assert.Null(report);
                    Assert.NotNull(completedReport);

                    report = completedReport;
                },
                CompilationMode = CompilationMode.Player
            });

            // we have 2 modules reporting ProjectSetting issues
            Assert.AreEqual(2, numModules);
            Assert.NotNull(report);
        }

        [Test]
        public void ProjectAuditor_Report_IsUpdated()
        {
            var projectAuditor = new Unity.ProjectAuditor.Editor.ProjectAuditor();
            var report = projectAuditor.Audit(new AnalysisParams
            {
                Categories = new[] { IssueCategory.ProjectSetting}
            });

            Assert.True(report.HasCategory(IssueCategory.ProjectSetting));
            Assert.Positive(report.FindByCategory(IssueCategory.ProjectSetting).Count);

            report.ClearIssues(IssueCategory.ProjectSetting);

            Assert.False(report.HasCategory(IssueCategory.ProjectSetting));
            Assert.Zero(report.FindByCategory(IssueCategory.ProjectSetting).Count);

            projectAuditor.Audit(new AnalysisParams
            {
                Categories = new[] { IssueCategory.ProjectSetting},
                ExistingReport = report
            });

            Assert.True(report.HasCategory(IssueCategory.ProjectSetting));
            Assert.Positive(report.FindByCategory(IssueCategory.ProjectSetting).Count);
        }
    }
}
