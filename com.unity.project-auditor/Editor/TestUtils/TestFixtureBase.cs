using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Unity.ProjectAuditor.Editor.AssemblyUtils;
using Unity.ProjectAuditor.Editor.Core;
using Unity.ProjectAuditor.Editor.Utils;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace Unity.ProjectAuditor.Editor.Tests.Common
{
    public abstract class TestFixtureBase
    {
        static readonly string s_TempSceneFilename = Path.Combine(TestAsset.TempAssetsFolder, "TestScene.unity");

        protected CodeOptimization m_CodeOptimization = CodeOptimization.Release;
        protected BuildTarget m_Platform = GetDefaultBuildTarget();
        protected string m_BuildPath;
        protected ProjectAuditor m_ProjectAuditor;
        protected AnalysisParams m_AnalysisParams;
        protected string m_AssemblyName = AssemblyInfo.DefaultAssemblyName;
        protected AndroidArchitecture m_OriginalTargetArchitecture;
        protected List<Rule> m_AdditionalRules = new List<Rule>();

        protected string m_SavedCompanyName;
        protected string m_SavedProductName;
        bool m_SavedBakeCollisionMeshes;
        bool m_SavedAnalyzeAfterBuild;

        protected ReportItem[] m_ReportItems;

        public static BuildTarget GetDefaultBuildTarget()
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

        protected TestFixtureBase()
        {
            m_SavedAnalyzeAfterBuild = UserPreferences.AnalyzeAfterBuild;
            UserPreferences.AnalyzeAfterBuild = false;

            DescriptorLibrary.Reset();

            m_ProjectAuditor = new ProjectAuditor();

            if (m_Platform == BuildTarget.Android)
            {
                m_OriginalTargetArchitecture = PlayerSettings.Android.targetArchitectures;
                if (m_OriginalTargetArchitecture == AndroidArchitecture.None)
                    PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARMv7;
            }

            m_SavedCompanyName = PlayerSettings.companyName;
            m_SavedProductName = PlayerSettings.productName;

            PlayerSettings.companyName = "DefaultCompany";
            PlayerSettings.productName = "ProjectName";

            m_SavedBakeCollisionMeshes = PlayerSettings.bakeCollisionMeshes;

            PlayerSettings.bakeCollisionMeshes = false;

            TestAsset.CreateTempFolder();
        }

        [OneTimeTearDown]
        public void Cleanup()
        {
            if (m_Platform == BuildTarget.Android)
            {
                PlayerSettings.Android.targetArchitectures = m_OriginalTargetArchitecture;
            }

            // restore player settings
            PlayerSettings.companyName = m_SavedCompanyName;
            PlayerSettings.productName = m_SavedProductName;
            PlayerSettings.bakeCollisionMeshes = m_SavedBakeCollisionMeshes;

            TestAsset.Cleanup();

            CleanupBuild();

            UserPreferences.AnalyzeAfterBuild = m_SavedAnalyzeAfterBuild;
        }

        protected void AnalyzeTestAssets()
        {
            m_ReportItems = Analyze(i =>
            {
                if (!i.IsIssue())
                    return false;
                if (i.Category == IssueCategory.ProjectSetting)
                    return true;
                return PathUtils.GetDirectoryName(i.RelativePath).Equals(TestAsset.TempAssetsFolder);
            });
        }

        protected ReportItem[] AnalyzeFiltered(Predicate<string> filterPredicate)
        {
            var foundIssues = new List<ReportItem>();

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

        protected ReportItem[] AnalyzeFiltered(Predicate<string> filterPredicate, IssueCategory category)
        {
            return AnalyzeFiltered(filterPredicate, new[] { category });
        }

        protected ReportItem[] AnalyzeFiltered(Predicate<string> filterPredicate, IssueCategory[] categories)
        {
            var foundIssues = new List<ReportItem>();
            var projectAuditor = new ProjectAuditor();
            var projectAuditorParams = new AnalysisParams()
            {
                AssemblyNames = new[] { m_AssemblyName },
                Categories = categories,
                OnIncomingIssues = issues =>
                {
                    var categoryIssues = issues.Where(issue => categories.Contains(issue.Category));
                    foundIssues.AddRange(categoryIssues);
                },
                Platform = m_Platform,
                AssetPathFilter = filterPredicate
            };

            projectAuditor.Audit(projectAuditorParams);

            return foundIssues.ToArray();
        }

        protected ReportItem[] Analyze(Func<ReportItem, bool> predicate = null)
        {
            ValidateTargetPlatform();

            var foundIssues = new List<ReportItem>();

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

        protected ReportItem[] Analyze(IssueCategory category, Func<ReportItem, bool> predicate = null)
        {
            ValidateTargetPlatform();

            var foundIssues = new List<ReportItem>();
            var projectAuditor = new ProjectAuditor();
            m_AnalysisParams = new AnalysisParams
            {
                AssemblyNames = new[] { m_AssemblyName },
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

        protected ReportItem[] AnalyzeAndFindAssetIssues(TestAsset testAsset,
            IssueCategory category = IssueCategory.Code)
        {
            return Analyze(category, i => i.RelativePath.Equals(testAsset.RelativePath));
        }

        protected ReportItem[] AnalyzeBuild(Func<ReportItem, bool> predicate = null, bool isDevelopment = true, string buildFileName = "test", Action preBuildAction = null, Action postBuildAction = null)
        {
            Build(isDevelopment, buildFileName, preBuildAction, postBuildAction);

            var res = Analyze(predicate);

            return res;
        }

        protected ReportItem[] AnalyzeBuild(IssueCategory category, Func<ReportItem, bool> predicate = null, bool isDevelopment = true, string buildFileName = "test", Action preBuildAction = null, Action postBuildAction = null)
        {
            Build(isDevelopment, buildFileName, preBuildAction, postBuildAction);

            var res = Analyze(category, predicate);

            return res;
        }

        protected void Build(bool isDevelopment = true, string buildFileName = "test", Action preBuildAction = null, Action postBuildAction = null)
        {
            CleanupBuild();

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

            Assert.True(buildReport.summary.result == BuildResult.Succeeded, "Build failed. Check the console for details.");
        }

        protected void CleanupBuild()
        {
            if (Directory.Exists(m_BuildPath))
                Directory.Delete(m_BuildPath, true);

            if (File.Exists(s_TempSceneFilename))
                AssetDatabase.DeleteAsset(s_TempSceneFilename);
        }

        protected ReportItem[] GetIssues()
        {
            return m_ReportItems.Where(i => i.IsIssue()).ToArray();
        }

        protected ReportItem[] GetIssuesForAsset(TestAsset testAsset)
        {
            return m_ReportItems.Where(i => i.IsIssue() && i.RelativePath == testAsset.RelativePath).ToArray();
        }

        protected void ValidateTargetPlatform()
        {
            Assert.IsTrue(BuildPipeline.IsBuildTargetSupported(BuildPipeline.GetBuildTargetGroup(m_Platform), m_Platform));
        }
    }
}
