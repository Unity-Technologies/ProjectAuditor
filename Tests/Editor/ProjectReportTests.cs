using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Unity.ProjectAuditor.Editor;
using UnityEngine;

namespace UnityEditor.ProjectAuditor.EditorTests
{
    class ProjectReportTests
    {
        TempAsset m_TempAsset;

        [OneTimeSetUp]
        public void SetUp()
        {
            m_TempAsset = new TempAsset("MyClass.cs", @"
using UnityEngine;
class MyClass
{
    void Dummy()
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
        public void NewReportIsValid()
        {
            var projectReport = new ProjectReport();
            Assert.Zero(projectReport.NumTotalIssues);
            Assert.Zero(projectReport.GetNumIssues(IssueCategory.Code));
            Assert.Zero(projectReport.GetNumIssues(IssueCategory.ProjectSettings));
        }

        [Test]
        public void IssueIsAddedToReport()
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
            Assert.AreEqual(0, projectReport.GetNumIssues(IssueCategory.ProjectSettings));
        }

        void AnalyzeAndExport(IssueCategory category, string path)
        {
            var config = ScriptableObject.CreateInstance<ProjectAuditorConfig>();
            config.AnalyzeEditorCode = false;

            var projectAuditor = new Unity.ProjectAuditor.Editor.ProjectAuditor(config);
            var projectReport = projectAuditor.Audit();
            var layout = projectAuditor.GetLayout(category);

            projectReport.ExportToCSV(path, layout);

            Assert.True(File.Exists(path));
        }

        [Test]
        public void CodesIssuesAreExportedAndFormatted()
        {
            // disabling stripEngineCode will be reported as a ProjectSettings issue
            PlayerSettings.stripEngineCode = false;

            var category = IssueCategory.Code;
            var path = string.Format("project-auditor-report-{0}.csv", category.ToString()).ToLower();
            AnalyzeAndExport(category, path);
            var issueFound = false;
            using (var file = new StreamReader(path))
            {
                var line = file.ReadLine();
                Assert.True(line.Equals("Issue,!,Area,Filename,Assembly"));

                var expectedIssueLine = "\"UnityEngine.Camera.allCameras\",\"Default\",\"Memory\",\"MyClass.cs:7\",\"Assembly-CSharp\"";
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
        public void SettingsIssuesAreExportedAndFormatted()
        {
            // disabling stripEngineCode will be reported as a ProjectSettings issue
            PlayerSettings.stripEngineCode = false;

            var category = IssueCategory.ProjectSettings;
            var path = string.Format("project-auditor-report-{0}.csv", category.ToString()).ToLower();
            AnalyzeAndExport(category, path);
            var issueFound = false;
            using (var file = new StreamReader(path))
            {
                var line = file.ReadLine();
                Assert.True(line.Equals("Issue,Area"));

                var expectedIssueLine = "\"Player: Engine Code Stripping\",\"BuildSize\"";
                while (file.Peek() >= 0)
                {
                    line = file.ReadLine();
                    if (line.Equals(expectedIssueLine))
                        issueFound = true;
                }
            }

            Assert.True(issueFound);
        }
    }
}
