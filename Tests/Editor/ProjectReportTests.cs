using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Unity.ProjectAuditor.Editor;
using Unity.ProjectAuditor.Editor.AssemblyUtils;
using Unity.ProjectAuditor.Editor.Utils;
using UnityEditor;
using UnityEngine;

namespace Unity.ProjectAuditor.EditorTests
{
    class ProjectReportTests
    {
#pragma warning disable 0414
        TempAsset m_TempAsset;
#pragma warning restore 0414

        [OneTimeSetUp]
        public void SetUp()
        {
            m_TempAsset = new TempAsset("MyClass.cs", @"
using UnityEngine;
class MyClass : MonoBehaviour
{
    void Update()
    {
        Debug.Log(Camera.allCameras.Length);
    }
}
");
        }

        [OneTimeTearDown]
        public void TearDown()
        {
            TempAsset.Cleanup();
        }

        [Test]
        public void ProjectReport_NewReport_IsValid()
        {
            var projectReport = new ProjectReport();
            Assert.Zero(projectReport.NumTotalIssues);
            Assert.Zero(projectReport.GetNumIssues(IssueCategory.Code));
            Assert.Zero(projectReport.GetNumIssues(IssueCategory.ProjectSetting));
        }

        [Test]
        public void ProjectReport_Issue_IsAdded()
        {
            var projectReport = new ProjectReport();
            var p = new ProblemDescriptor
                (
                102001,
                "test",
                Area.CPU,
                "this is not actually a problem",
                "do nothing"
                );

            projectReport.AddIssue(new ProjectIssue
                (
                    p,
                    "dummy issue",
                    IssueCategory.Code
                )
            );

            Assert.AreEqual(1, projectReport.NumTotalIssues);
            Assert.AreEqual(1, projectReport.GetNumIssues(IssueCategory.Code));
            Assert.AreEqual(0, projectReport.GetNumIssues(IssueCategory.ProjectSetting));
        }

        ProjectIssue[] AnalyzeAndExport(IssueCategory category, string path, Func<ProjectIssue, bool> predicate = null)
        {
            var config = ScriptableObject.CreateInstance<ProjectAuditorConfig>();
            config.CompilationMode = CompilationMode.Player;

            var projectAuditor = new Unity.ProjectAuditor.Editor.ProjectAuditor(config);
            var projectReport = projectAuditor.Audit();
            var layout = projectAuditor.GetLayout(category);

            projectReport.ExportToCSV(path, layout, predicate);

            Assert.True(File.Exists(path));

            return projectReport.GetIssues(category);
        }

        [Test]
        public void ProjectReport_CodeIssues_AreExportedAndFormatted()
        {
            var category = IssueCategory.Code;
            var path = string.Format("project-auditor-report-{0}.csv", category.ToString()).ToLower();
            AnalyzeAndExport(category, path);
            var issueFound = false;
            using (var file = new StreamReader(path))
            {
                var line = file.ReadLine();
                Assert.AreEqual("Issue,Critical,Area,Filename,Assembly", line);

                var expectedIssueLine = "\"'UnityEngine.Camera.allCameras' usage\",\"True\",\"Memory\",\"MyClass.cs:7\",\"Assembly-CSharp\"";
                while (file.Peek() >= 0)
                {
                    line = file.ReadLine();
                    if (line.Equals(expectedIssueLine))
                        issueFound = true;
                }
            }

            Assert.True(issueFound);
        }

        [Test]
        public void ProjectReport_CodeIssues_AreFilteredAndExported()
        {
            var category = IssueCategory.Code;
            var path = string.Format("project-auditor-report-{0}.csv", category.ToString()).ToLower();
            AnalyzeAndExport(category, path, issue =>
            {
                return issue.description.StartsWith("Conversion");
            });
            var issueFound = false;
            using (var file = new StreamReader(path))
            {
                var line = file.ReadLine();
                Assert.AreEqual("Issue,Critical,Area,Filename,Assembly", line);

                var expectedIssueLine = "\"Conversion from value type 'Int32' to ref type\",\"True\",\"Memory\",\"MyClass.cs:7\",\"Assembly-CSharp\"";
                while (file.Peek() >= 0)
                {
                    line = file.ReadLine();
                    if (line.Equals(expectedIssueLine))
                        issueFound = true;
                }
            }

            Assert.True(issueFound);
        }

        [Test]
        public void ProjectReport_SettingsIssues_AreExportedAndFormatted()
        {
            var bakeCollisionMeshes = PlayerSettings.bakeCollisionMeshes;
            PlayerSettings.bakeCollisionMeshes = false;

            var category = IssueCategory.ProjectSetting;
            var path = string.Format("project-auditor-report-{0}.csv", category.ToString()).ToLower();
            var issues = AnalyzeAndExport(category, path);
            var issue = issues.FirstOrDefault(i => i.descriptor.method.Equals("bakeCollisionMeshes"));
            var expectedIssueLine = string.Format("\"{0}\",\"{1}\"", issue.description, issue.descriptor.GetAreasSummary());

            var issueFound = false;
            using (var file = new StreamReader(path))
            {
                var line = file.ReadLine();
                Assert.AreEqual("Issue,Area", line, "Header was: " + line);

                while (file.Peek() >= 0)
                {
                    line = file.ReadLine();
                    if (line.Equals(expectedIssueLine))
                        issueFound = true;
                }
            }

            Assert.True(issueFound);

            PlayerSettings.bakeCollisionMeshes = bakeCollisionMeshes;
        }
    }
}
