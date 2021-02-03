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

        [Test]
        public void ReportIsExportedAndFormatted()
        {
            // disabling stripEngineCode will be reported as a ProjectSettings issue
            PlayerSettings.stripEngineCode = false;

            var config = ScriptableObject.CreateInstance<ProjectAuditorConfig>();
            config.AnalyzeEditorCode = false;

            var projectAuditor = new Unity.ProjectAuditor.Editor.ProjectAuditor(config);

            var projectReport = projectAuditor.Audit();

            const string path = "ProjectAuditor_Report.csv";
            projectReport.ExportToCSV(path);
            Assert.True(File.Exists(path));

            var settingsIssue = projectReport.GetIssues(IssueCategory.ProjectSettings)
                .First(i => i.descriptor.method.Equals("stripEngineCode"));
            var scriptIssue = projectReport.GetIssues(IssueCategory.Code)
                .First(i => i.relativePath.Equals(m_TempAsset.relativePath));

            var settingsIssueFound = false;
            var scriptIssueFound = false;
            using (var file = new StreamReader(path))
            {
                var line = file.ReadLine();
                Assert.True(line.Equals(ProjectReport.HeaderForCSV()));

                var expectedSettingsIssueLine = ProjectReport.FormatIssueForCSV(settingsIssue);
                var expectedScriptIssueLine = ProjectReport.FormatIssueForCSV(scriptIssue);
                while (file.Peek() >= 0)
                {
                    line = file.ReadLine();
                    if (line.Equals(expectedSettingsIssueLine)) settingsIssueFound = true;
                    if (line.Equals(expectedScriptIssueLine)) scriptIssueFound = true;
                }
            }

            Assert.True(settingsIssueFound);
            Assert.True(scriptIssueFound);
        }

        [Test]
        public void FilteredReportIsExported()
        {
            // disabling stripEngineCode will be reported as a ProjectSettings issue
            PlayerSettings.stripEngineCode = false;

            var projectAuditor = new Unity.ProjectAuditor.Editor.ProjectAuditor();
            var projectReport = projectAuditor.Audit();

            const string path = "ProjectAuditor_Report.csv";

            // let's assume we are only interested in exporting project settings
            projectReport.ExportToCSV(path, issue => issue.category == IssueCategory.ProjectSettings);
            Assert.True(File.Exists(path));

            using (var file = new StreamReader(path))
            {
                var line = file.ReadLine();
                Assert.True(line.Equals(ProjectReport.HeaderForCSV()));

                while (file.Peek() >= 0)
                {
                    line = file.ReadLine();
                    Assert.True(line.StartsWith(IssueCategory.ProjectSettings.ToString()));
                }
            }
        }
    }
}
