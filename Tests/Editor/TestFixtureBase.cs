using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Unity.ProjectAuditor.Editor;
using Unity.ProjectAuditor.Editor.AssemblyUtils;
using Unity.ProjectAuditor.Editor.Modules;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Unity.ProjectAuditor.EditorTests
{
    public abstract class TestFixtureBase
    {
        protected CodeOptimization m_CodeOptimization = CodeOptimization.Release;
        protected ProjectAuditorConfig m_Config;

        [OneTimeSetUp]
        public void FixtureSetUp()
        {
            m_Config = ScriptableObject.CreateInstance<ProjectAuditorConfig>();
            m_Config.AnalyzeInBackground = false;
        }

        [OneTimeTearDown]
        public void FixtureTearDown()
        {
            TempAsset.Cleanup();
        }

        protected ProjectIssue[] Analyze(Func<ProjectIssue, bool> predicate = null)
        {
            var foundIssues = new List<ProjectIssue>();

            var projectAuditor = new Unity.ProjectAuditor.Editor.ProjectAuditor(m_Config);
            var projectAuditorParams = new ProjectAuditorParams
            {
                codeOptimization = m_CodeOptimization,
                onIssueFound = issue => {
                    if (predicate == null || predicate(issue))
                        foundIssues.Add(issue);
                },
            };
            projectAuditor.Audit(projectAuditorParams);

            return foundIssues.ToArray();
        }

        protected ProjectIssue[] Analyze(IssueCategory category, Func<ProjectIssue, bool> predicate = null)
        {
            var foundIssues = new List<ProjectIssue>();
            var projectAuditor = new Unity.ProjectAuditor.Editor.ProjectAuditor(m_Config);
            var module = projectAuditor.GetModule(category);
            var projectAuditorParams = new ProjectAuditorParams
            {
                assemblyNames = new[] { AssemblyInfo.DefaultAssemblyName},
                onIssueFound = issue => {
                    if (issue.category != category)
                        return;

                    if (predicate == null || predicate(issue))
                        foundIssues.Add(issue);
                }
            };

            module.Audit(projectAuditorParams);

            return foundIssues.ToArray();
        }

        protected ProjectIssue[] AnalyzeAndFindAssetIssues(TempAsset tempAsset,
            IssueCategory category = IssueCategory.Code)
        {
            return Analyze(category, i => i.relativePath.Equals(tempAsset.relativePath));
        }

        protected ProjectIssue[] AnalyzeBuild(Func<ProjectIssue, bool> predicate = null)
        {
            Build();

            return Analyze(predicate);
        }

        protected ProjectIssue[] AnalyzeBuild(IssueCategory category, Func<ProjectIssue, bool> predicate = null)
        {
            Build();

            return Analyze(category, predicate);
        }

        static void Build()
        {
            const string tempSceneFilename = "Assets/TestScene.unity";
            // We must save the scene or the build will fail https://unity.slack.com/archives/C3F85MBDL/p1615991512002200
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), tempSceneFilename);

            var buildPath = FileUtil.GetUniqueTempPathInProject();
            Directory.CreateDirectory(buildPath);
            var buildPlayerOptions = new BuildPlayerOptions
            {
                scenes = new string[] {},
                locationPathName = Path.Combine(buildPath, "test"),
                target = EditorUserBuildSettings.activeBuildTarget,
                targetGroup = BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget),
                options = BuildOptions.Development
            };
            var buildReport = BuildPipeline.BuildPlayer(buildPlayerOptions);

            Assert.True(buildReport.summary.result == BuildResult.Succeeded);

            Directory.Delete(buildPath, true);

            AssetDatabase.DeleteAsset(tempSceneFilename);
        }
    }
}
