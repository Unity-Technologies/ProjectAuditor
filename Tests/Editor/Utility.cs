using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Unity.ProjectAuditor.Editor;
using Unity.ProjectAuditor.Editor.Auditors;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace UnityEditor.ProjectAuditor.EditorTests
{
    public static class Utility
    {
        public static ProjectIssue[] Analyze(IssueCategory category)
        {
            var projectAuditor = new Unity.ProjectAuditor.Editor.ProjectAuditor();
            var module = projectAuditor.GetModule(category);
            var config = ScriptableObject.CreateInstance<ProjectAuditorConfig>();
            config.AnalyzeInBackground = false;
            module.Initialize(config);

            var foundIssues = new List<ProjectIssue>();
            module.Audit(issue => {
                foundIssues.Add(issue);
            });

            return foundIssues.Where(i => i.category == category).ToArray();
        }

        public static ProjectIssue[] AnalyzeAndFindAssetIssues(TempAsset tempAsset, IssueCategory category = IssueCategory.Code)
        {
            var foundIssues = Analyze(category);

            return foundIssues.Where(i => i.relativePath.Equals(tempAsset.relativePath)).ToArray();
        }

        public static ProjectReport AnalyzeBuild()
        {
            // We must save the scene or the build will fail https://unity.slack.com/archives/C3F85MBDL/p1615991512002200
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), "Assets/UntitledScene.unity");

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

            AssetDatabase.DeleteAsset("Assets/UntitledScene.unity");

            var projectAuditor = new Unity.ProjectAuditor.Editor.ProjectAuditor();
            var projectReport = projectAuditor.Audit();
            return projectReport;
        }
    }
}
