using System.IO;
using System.Linq;
using NUnit.Framework;
using Unity.ProjectAuditor.Editor;

namespace UnityEditor.ProjectAuditor.EditorTests
{
    internal class ProjectReportTests
    {
        private ScriptResource m_ScriptResource;

        [OneTimeSetUp]
        public void SetUp()
        {
            m_ScriptResource = new ScriptResource("MyClass.cs", @"
using UnityEngine;
class MyClass
{
    void Dummy()
    {
        // Accessing Camera.main property is not recommended and will be reported as a possible performance problem.
        Debug.Log(Camera.main.name);
    }
}
");
        }

        [OneTimeTearDown]
        public void TearDown()
        {
            m_ScriptResource.Delete();
        }

        [Test]
        public void NewReportIsValid()
        {
            var projectReport = new ProjectReport();
            Assert.Zero(projectReport.NumTotalIssues);
            Assert.Zero(projectReport.GetNumIssues(IssueCategory.ApiCalls));
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
                    IssueCategory.ApiCalls
                )
            );

            Assert.AreEqual(1, projectReport.NumTotalIssues);
            Assert.AreEqual(1, projectReport.GetNumIssues(IssueCategory.ApiCalls));
            Assert.AreEqual(0, projectReport.GetNumIssues(IssueCategory.ProjectSettings));
        }

        [Test]
        public void ReportIsExportedAndFormatted()
        {
            // disabling stripEngineCode will be reported as a ProjectSettings issue
            PlayerSettings.stripEngineCode = false;

            var projectAuditor = new Unity.ProjectAuditor.Editor.ProjectAuditor();

            var projectReport = projectAuditor.Audit();

            const string path = "ProjectAuditor_Report.csv";
            projectReport.Export(path);
            Assert.True(File.Exists(path));

            var settingsIssue = projectReport.GetIssues(IssueCategory.ProjectSettings)
                .First(i => i.descriptor.method.Equals("stripEngineCode"));
            var scriptIssue = projectReport.GetIssues(IssueCategory.ApiCalls)
                .First(i => i.relativePath.Equals(m_ScriptResource.relativePath));

            var settingsIssueFound = false;
            var scriptIssueFound = false;
            using (var file = new StreamReader(path))
            {
                var line = file.ReadLine();
                Assert.True(line.Equals("Issue,Message,Area,Path"));

                var expectedSettingsIssueLine = string.Format("\"{0}\",\"{1}\",{2},{3}", settingsIssue.descriptor.description,
                    settingsIssue.description,
                    settingsIssue.descriptor.area, settingsIssue.relativePath);
                var expectedScriptIssueLine = string.Format("\"{0}\",\"{1}\",{2},{3}:{4}", scriptIssue.descriptor.description,
                    scriptIssue.description,
                    scriptIssue.descriptor.area, scriptIssue.relativePath, scriptIssue.line);
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
    }
}
