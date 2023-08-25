using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Unity.ProjectAuditor.Editor.AssemblyUtils;
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
        protected BuildTarget m_Platform = EditorUserBuildSettings.activeBuildTarget;
        protected ProjectAuditorConfig m_Config;
        protected string m_BuildPath;
        protected Editor.ProjectAuditor m_ProjectAuditor;
        protected ProjectAuditorDiagnosticParamsProvider m_DiagnosticParamsProvider;

        [OneTimeSetUp]
        public void FixtureSetUp()
        {
            m_Config = ScriptableObject.CreateInstance<ProjectAuditorConfig>();
            m_Config.AnalyzeInBackground = false;

            m_ProjectAuditor = new Unity.ProjectAuditor.Editor.ProjectAuditor(m_Config);

            m_DiagnosticParamsProvider = new ProjectAuditorDiagnosticParamsProvider();
            m_DiagnosticParamsProvider.Initialize();

            TestAsset.CreateTempFolder();
        }

        [OneTimeTearDown]
        public void FixtureTearDown()
        {
            TestAsset.Cleanup();
        }

        protected ProjectIssue[] Analyze(Func<ProjectIssue, bool> predicate = null)
        {
            var foundIssues = new List<ProjectIssue>();

            var projectAuditorParams = new ProjectAuditorParams
            {
                codeOptimization = m_CodeOptimization,
                onIncomingIssues = issues =>
                {
                    foundIssues.AddRange(predicate == null ? issues : issues.Where(predicate));
                },
                platform = m_Platform,
                diagnosticParams = m_DiagnosticParamsProvider.GetCurrentParams()
            };
            m_ProjectAuditor.Audit(projectAuditorParams);

            return foundIssues.ToArray();
        }

        protected ProjectIssue[] Analyze(IssueCategory category, Func<ProjectIssue, bool> predicate = null)
        {
            var foundIssues = new List<ProjectIssue>();
            var projectAuditor = new Unity.ProjectAuditor.Editor.ProjectAuditor(m_Config);
            var projectAuditorParams = new ProjectAuditorParams
            {
                assemblyNames = new[] { "Assembly-CSharp" },
                categories = new[] { category},
                onIncomingIssues = issues =>
                {
                    var categoryIssues = issues.Where(issue => issue.category == category);

                    foundIssues.AddRange(predicate == null ? categoryIssues : categoryIssues.Where(predicate));
                },
                platform = m_Platform,
                diagnosticParams = m_DiagnosticParamsProvider.GetCurrentParams()
            };

            projectAuditor.Audit(projectAuditorParams);

            return foundIssues.ToArray();
        }

        protected ProjectIssue[] AnalyzeAndFindAssetIssues(TestAsset testAsset,
            IssueCategory category = IssueCategory.Code)
        {
            return Analyze(category, i => i.relativePath.Equals(testAsset.relativePath));
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
    }
}
