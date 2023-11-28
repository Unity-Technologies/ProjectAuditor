using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Unity.ProjectAuditor.Editor.AssemblyUtils;
using Unity.ProjectAuditor.Editor.Diagnostic;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Unity.ProjectAuditor.Editor.Tests.Common
{
    public abstract class TestFixtureBase
    {
        static readonly string s_TempSceneFilename = Path.Combine(TestAsset.TempAssetsFolder, "TestScene.unity");

        protected CodeOptimization m_CodeOptimization = CodeOptimization.Release;
        protected BuildTarget m_Platform = GetStandaloneBuildTarget();
        protected string m_BuildPath;
        protected ProjectAuditor m_ProjectAuditor;
        protected AnalysisParams m_AnalysisParams;
        protected AndroidArchitecture m_OriginalTargetArchitecture;
        protected List<Diagnostic.Rule> m_AdditionalRules = new List<Diagnostic.Rule>();
        protected string m_OriginalCompanyName;
        protected string m_OriginalProductName;

        bool m_SavedAnalyzeInBackground;

        public static BuildTarget GetStandaloneBuildTarget()
        {
#if UNITY_EDITOR_WIN
            return BuildTarget.StandaloneWindows64;
#elif UNITY_EDITOR_OSX
            return BuildTarget.StandaloneOSX;
#elif UNITY_EDITOR_LINUX
            return BuildTarget.StandaloneLinux64;
#else
            // Log a warning or throw an exception
            return BuildTarget.NoTarget;  // NoTarget is an invalid BuildTarget, for demonstration purposes.
#endif
        }

        [OneTimeSetUp]
        public void FixtureSetUp()
        {
            m_SavedAnalyzeInBackground = UserPreferences.AnalyzeInBackground;
            UserPreferences.AnalyzeInBackground = false;

            DescriptorLibrary.Reset();

            m_ProjectAuditor = new ProjectAuditor();

            if (m_Platform == BuildTarget.Android)
            {
                m_OriginalTargetArchitecture = PlayerSettings.Android.targetArchitectures;
                if (m_OriginalTargetArchitecture == AndroidArchitecture.None)
                    PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARMv7;
            }

            m_OriginalCompanyName = PlayerSettings.companyName;
            m_OriginalProductName = PlayerSettings.productName;

            PlayerSettings.companyName = "DefaultCompany";
            PlayerSettings.productName = "ProjectName";

            TestAsset.CreateTempFolder();
        }

        [OneTimeTearDown]
        public void FixtureTearDown()
        {
            if (m_Platform == BuildTarget.Android)
            {
                PlayerSettings.Android.targetArchitectures = m_OriginalTargetArchitecture;
            }

            PlayerSettings.companyName = m_OriginalCompanyName;
            PlayerSettings.productName = m_OriginalProductName;

            TestAsset.Cleanup();

            UserPreferences.AnalyzeInBackground = m_SavedAnalyzeInBackground;
        }

        protected ProjectIssue[] AnalyzeFiltered(Predicate<string> filterPredicate)
        {
            var foundIssues = new List<ProjectIssue>();

            var projectAuditorParams = new AnalysisParams()
            {
                CodeOptimization = m_CodeOptimization,
                OnIncomingIssues = foundIssues.AddRange,
                Platform = m_Platform,
                AssetPathFilter = filterPredicate
            };
            m_ProjectAuditor.Audit(projectAuditorParams);

            return foundIssues.ToArray();
        }


        protected ProjectIssue[] AnalyzeFiltered(Predicate<string> filterPredicate, IssueCategory category)
        {
            return AnalyzeFiltered(filterPredicate, new [] { category });
        }

        protected ProjectIssue[] AnalyzeFiltered(Predicate<string> filterPredicate, IssueCategory[] categories)
        {
            var foundIssues = new List<ProjectIssue>();
            var projectAuditor = new ProjectAuditor();
            var projectAuditorParams = new AnalysisParams()
            {
                AssemblyNames = new[] { "Assembly-CSharp" },
                Categories = categories,
                OnIncomingIssues = issues =>
                {
                    var categoryIssues = issues.Where(issue => categories.Contains(issue.category));
                    foundIssues.AddRange(categoryIssues);
                },
                Platform = m_Platform,
                AssetPathFilter = filterPredicate
            };

            projectAuditor.Audit(projectAuditorParams);

            return foundIssues.ToArray();
        }

        protected ProjectIssue[] Analyze(Func<ProjectIssue, bool> predicate = null)
        {
            ValidateTargetPlatform();

            var foundIssues = new List<ProjectIssue>();

            m_AnalysisParams = new AnalysisParams
            {
                CodeOptimization = m_CodeOptimization,
                OnIncomingIssues = issues =>
                {
                    foundIssues.AddRange(predicate == null ? issues : issues.Where(predicate));
                },
                Platform = m_Platform
            }.WithAdditionalDiagnosticRules(m_AdditionalRules);

            m_ProjectAuditor.Audit(m_AnalysisParams);

            return foundIssues.ToArray();
        }

        protected ProjectIssue[] Analyze(IssueCategory category, Func<ProjectIssue, bool> predicate = null)
        {
            ValidateTargetPlatform();

            var foundIssues = new List<ProjectIssue>();
            var projectAuditor = new ProjectAuditor();
            m_AnalysisParams = new AnalysisParams
            {
                AssemblyNames = new[] { "Assembly-CSharp" },
                Categories = new[] { category},
                OnIncomingIssues = issues =>
                {
                    var categoryIssues = issues.Where(issue => issue.Category == category);

                    foundIssues.AddRange(predicate == null ? categoryIssues : categoryIssues.Where(predicate));
                },
                Platform = m_Platform
            }.WithAdditionalDiagnosticRules(m_AdditionalRules);

            projectAuditor.Audit(m_AnalysisParams);

            return foundIssues.ToArray();
        }

        protected ProjectIssue[] AnalyzeAndFindAssetIssues(TestAsset testAsset,
            IssueCategory category = IssueCategory.Code)
        {
            return Analyze(category, i => i.RelativePath.Equals(testAsset.relativePath));
        }

        protected ProjectIssue[] AnalyzeBuild(Func<ProjectIssue, bool> predicate = null, bool isDevelopment = true, string buildFileName = "test", Action preBuildAction = null, Action postBuildAction = null)
        {
            Build(isDevelopment, buildFileName, preBuildAction, postBuildAction);

            var res = Analyze(predicate);

            CleanupBuild();

            return res;
        }

        protected ProjectIssue[] AnalyzeBuild(IssueCategory category, Func<ProjectIssue, bool> predicate = null, bool isDevelopment = true, string buildFileName = "test", Action preBuildAction = null, Action postBuildAction = null)
        {
            Build(isDevelopment, buildFileName, preBuildAction, postBuildAction);

            var res = Analyze(category, predicate);

            CleanupBuild();

            return res;
        }

        protected void Build(bool isDevelopment = true, string buildFileName = "test", Action preBuildAction = null, Action postBuildAction = null)
        {
            ValidateTargetPlatform();

            // We must save the scene or the build will fail https://unity.slack.com/archives/C3F85MBDL/p1615991512002200
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), s_TempSceneFilename);

            m_BuildPath = FileUtil.GetUniqueTempPathInProject();
            Directory.CreateDirectory(m_BuildPath);

            var options = isDevelopment ? BuildOptions.Development : BuildOptions.None;
#if UNITY_2021_2_OR_NEWER
            options |= BuildOptions.CleanBuildCache;
#endif
            var buildPlayerOptions = new BuildPlayerOptions
            {
                scenes = new string[] {},
                locationPathName = Path.Combine(m_BuildPath, buildFileName),
                target = m_Platform,
                targetGroup = BuildPipeline.GetBuildTargetGroup(m_Platform),
                options = options
            };

            preBuildAction?.Invoke();

            var buildReport = BuildPipeline.BuildPlayer(buildPlayerOptions);

            postBuildAction?.Invoke();

            Assert.True(buildReport.summary.result == BuildResult.Succeeded);
        }

        protected void CleanupBuild()
        {
            Directory.Delete(m_BuildPath, true);

            AssetDatabase.DeleteAsset(s_TempSceneFilename);
        }

        protected void ValidateTargetPlatform()
        {
            Assert.IsTrue(BuildPipeline.IsBuildTargetSupported(BuildPipeline.GetBuildTargetGroup(m_Platform), m_Platform));
        }
    }
}
