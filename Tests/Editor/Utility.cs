using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Unity.ProjectAuditor.Editor;
using Unity.ProjectAuditor.Editor.Modules;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Unity.ProjectAuditor.EditorTests
{
    public static class Utility
    {
        public static ProjectIssue[] Analyze(Func<ProjectIssue, bool> predicate = null)
        {
            var config = ScriptableObject.CreateInstance<ProjectAuditorConfig>();
            config.AnalyzeInBackground = false;

            var projectAuditor = new Unity.ProjectAuditor.Editor.ProjectAuditor(config);
            var projectReport = projectAuditor.Audit();

            return projectReport.GetAllIssues().ToArray();
        }

        public static ProjectIssue[] Analyze(IssueCategory category, Func<ProjectIssue, bool> predicate = null)
        {
            var config = ScriptableObject.CreateInstance<ProjectAuditorConfig>();
            config.AnalyzeInBackground = false;

            var projectAuditor = new Unity.ProjectAuditor.Editor.ProjectAuditor(config);
            var module = projectAuditor.GetModule(category);
            var issues = module.Audit();

            return issues.Where(i => i.category == category && (predicate == null || predicate(i))).ToArray();
        }

        public static ProjectIssue[] AnalyzeAndFindAssetIssues(TempAsset tempAsset,
            IssueCategory category = IssueCategory.Code)
        {
            return Analyze(category, i => i.relativePath.Equals(tempAsset.relativePath));
        }

        public static ProjectIssue[] AnalyzeBuild(Func<ProjectIssue, bool> predicate = null)
        {
            Build();

            return Analyze(predicate);
        }

        public static ProjectIssue[] AnalyzeBuild(IssueCategory category, Func<ProjectIssue, bool> predicate = null)
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
