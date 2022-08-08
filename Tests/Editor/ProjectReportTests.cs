using System;
using System.Collections.Generic;
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
    class ProjectReportTests : TestFixtureBase
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
                "TD2001",
                "test",
                Area.CPU,
                "this is not actually a problem",
                "do nothing"
                );

            projectReport.AddIssue(new ProjectIssue
                (
                    IssueCategory.Code,
                    p,
                    "dummy issue"
                )
            );

            Assert.AreEqual(1, projectReport.NumTotalIssues);
            Assert.AreEqual(1, projectReport.GetNumIssues(IssueCategory.Code));
            Assert.AreEqual(0, projectReport.GetNumIssues(IssueCategory.ProjectSetting));
        }

        IReadOnlyCollection<ProjectIssue> AnalyzeAndExport(IssueCategory category, string path, string format, Func<ProjectIssue, bool> predicate = null)
        {
            var config = ScriptableObject.CreateInstance<ProjectAuditorConfig>();
            config.CompilationMode = CompilationMode.Player;

            var projectAuditor = new Unity.ProjectAuditor.Editor.ProjectAuditor(config);
            var projectReport = projectAuditor.Audit();
            var layout = projectAuditor.GetLayout(category);

            switch (format)
            {
                case "csv":
                    projectReport.ExportToCSV(path, layout, predicate);
                    break;
                case "html":
                    projectReport.ExportToHTML(path, layout, predicate);
                    break;
                default:
                    break;
            }

            Assert.True(File.Exists(path));

            return projectReport.GetIssues(category);
        }

        [Test]
        public void ProjectReport_CodeIssues_AreExportedAndFormatted()
        {
            var category = IssueCategory.Code;
            var path = string.Format("project-auditor-report-{0}.csv", category.ToString()).ToLower();
            AnalyzeAndExport(category ,path, "csv");
            var issueFound = false;
            using (var file = new StreamReader(path))
            {
                var line = file.ReadLine();
                Assert.AreEqual("Issue,Critical,Area,Filename,Assembly,Descriptor", line);

                var expectedIssueLine = "\"'UnityEngine.Camera.allCameras' usage\",\"True\",\"Memory\",\"MyClass.cs:7\",\"Assembly-CSharp\",\"UnityEngine.Camera.allCameras\"";
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
            AnalyzeAndExport(category,path, "csv", issue =>
            {
                return issue.description.StartsWith("Conversion");
            });
            var issueFound = false;
            using (var file = new StreamReader(path))
            {
                var line = file.ReadLine();
                Assert.AreEqual("Issue,Critical,Area,Filename,Assembly,Descriptor", line);

                var expectedIssueLine = "\"Conversion from value type 'Int32' to ref type\",\"True\",\"Memory\",\"MyClass.cs:7\",\"Assembly-CSharp\",\"Boxing Allocation\"";
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
        //[Ignore("not finish filtered and exported")]
        public void ProjectReprt_Codeissues_AreFilteredAndExported_HTML() {
            IssueCategory category = IssueCategory.Code;
            string path = string.Format("project-auditor-report-{0}.html", category.ToString()).ToLower();
            AnalyzeAndExport(category, path, "html", issue =>
            {
                return issue.description.StartsWith("Conversion");
            });
            //AnalyzeAndExport(category, path, "html");
            bool issueFound = false;
            bool filterCorrect = true;
            using (StreamReader file = new StreamReader(path)) {
                var line = file.ReadLine();
                //Assert.AreEqual("<html>", line);
                line = file.ReadLine();
                //Assert.AreEqual("<body>", line);
                line = file.ReadLine();
                //Assert.AreEqual("<table width='50%' cellpadding='10' style='margin-top:10px' cellspacing='3' border='1' rules='all'>", line);
                line = file.ReadLine();
                //Assert.AreEqual("<tr>", line);
                line = file.ReadLine();
                //Assert.AreEqual("<th>Issue</th>", line);
                line = file.ReadLine();
                //Assert.AreEqual("<th>Critical</th>", line);
                line = file.ReadLine();
                //Assert.AreEqual("<th>Area</th>", line);
                line = file.ReadLine();
                //Assert.AreEqual("<th>Area</th>", line);
                line = file.ReadLine();
                //Assert.AreEqual("<th>Area</th>", line);
                line = file.ReadLine();
                //Assert.AreEqual("<th>Area</th>", line);
                line = file.ReadLine();
                //Assert.AreEqual("</tr>", line);
                
                while (file.Peek() > -1) {
                    line = file.ReadLine();
                    if (!line.Equals("</body>"))
                    {
                        int index = 0;
                        if (line.Equals("<tr>"))
                        {
                            index++;
                        }
                        line = file.ReadLine();
                        //if (line.Equals($"<td>Conversion from value type 'Int32' to ref type</td>"))
                        if (line.Contains("Conversion"))
                        {
                            if (line.Equals($"<td>Conversion from value type 'Int32' to ref type</td>"))
                            {
                                index++;
                            }
                        }
                        else
                        {
                            filterCorrect = false;
                        }
                        line = file.ReadLine();
                        if (line.Equals($"<td>True</td>"))
                        {
                            index++;
                        }
                        line = file.ReadLine();
                        if (line.Equals($"<td>Memory</td>"))
                        {
                            index++;
                        }
                        line = file.ReadLine();
                        if (line.Equals($"<td>MyClass.cs:7</td>"))
                        {
                            index++;
                        }
                        line = file.ReadLine();
                        if (line.Equals($"<td>Assembly-CSharp</td>"))
                        {
                            index++;
                        }
                        line = file.ReadLine();
                        if (line.Equals($"<td>Boxing Allocation</td>"))
                        {
                            index++;
                        }
                        line = file.ReadLine();
                        if (line.Equals("</tr>"))
                        {
                            index++;
                        }
                        if (index == 8)
                        {
                            issueFound = true;
                        }
                    }
                    else
                    {
                        line = file.ReadLine();
                        //if (line.Equals("</html>"))
                        //{
                        //    formatCorrect = true;
                        //}
                    }
                }
                Assert.True(issueFound && filterCorrect);
            }
        }

        [Test]
        public void ProjectReport_SettingsIssues_AreExportedAndFormatted()
        {
            var bakeCollisionMeshes = PlayerSettings.bakeCollisionMeshes;
            PlayerSettings.bakeCollisionMeshes = false;

            var category = IssueCategory.ProjectSetting;
            var path = string.Format("project-auditor-report-{0}.csv", category.ToString()).ToLower();
            var issues = AnalyzeAndExport(category,  path,"csv");
            var issue = issues.FirstOrDefault(i => i.descriptor.method.Equals("bakeCollisionMeshes"));
            var expectedIssueLine = $"\"{issue.description}\",\"{issue.descriptor.GetAreasSummary()}\",\"{issue.relativePath}\"";

            var issueFound = false;
            using (var file = new StreamReader(path))
            {
                var line = file.ReadLine();
                Assert.AreEqual("Issue,Area,Settings", line, "Header was: " + line);
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

        [Test]
        //[Ignore("HTMLExporter")]
        public void ProjectReport_SettingsIssues_AreExportedAndFormatted_HTML() {
            bool bakeCollisionMeshes = PlayerSettings.bakeCollisionMeshes;
            PlayerSettings.bakeCollisionMeshes = false;

            IssueCategory category = IssueCategory.ProjectSetting;
            string path = string.Format("project-auditor-report-{0}.html", category.ToString().ToLower());
            IReadOnlyCollection<ProjectIssue> issues = AnalyzeAndExport(category, path, "html");
            ProjectIssue issue =  issues.FirstOrDefault(i => i.descriptor.method.Equals("bakeCollisionMeshes"));
            string expectedIssueLine = $"\"{issue.description}\",\"{issue.descriptor.GetAreasSummary()}\",\"{issue.relativePath}\"";

            bool issueFound = false;
            bool formatCorrect = false;
            using(StreamReader file = new StreamReader(path))
            {
                var line = file.ReadLine();
                Assert.AreEqual("<html>", line);
                line = file.ReadLine();
                Assert.AreEqual("<body>", line);
                line = file.ReadLine();
                Assert.AreEqual("<table width='50%' cellpadding='10' style='margin-top:10px' cellspacing='3' border='1' rules='all'>", line);
                line = file.ReadLine();
                Assert.AreEqual("<tr>", line);
                line = file.ReadLine();
                Assert.AreEqual("<th>Issue</th>", line);
                line = file.ReadLine();
                Assert.AreEqual("<th>Area</th>", line);
                line = file.ReadLine();
                Assert.AreEqual("<th>Settings</th>", line);
                line = file.ReadLine();
                Assert.AreEqual("</tr>", line);

                while (file.Peek() > -1) {

                    line = file.ReadLine();
                    if (!line.Equals("</body>"))
                    {
                        int index = 0;
                        if (line.Equals("<tr>"))
                        {
                            index++;
                        }
                        line = file.ReadLine();
                        if (line.Equals($"<td>{issue.description}</td>"))
                        {
                            index++;
                        }
                        line = file.ReadLine();
                        if (line.Equals($"<td>{issue.descriptor.GetAreasSummary()}</td>"))
                        {
                            index++;
                        }
                        line = file.ReadLine();
                        if (line.Equals($"<td>{issue.relativePath}</td>"))
                        {
                            index++;
                        }
                        line = file.ReadLine();
                        if (line.Equals("</tr>"))
                        {
                            index++;
                        }
                        if (index == 5)
                        {
                            issueFound = true;
                        }
                    }
                    else {
                        line = file.ReadLine();
                        if (line.Equals("</html>")) {
                            formatCorrect = true;
                        }
                    }
                }
            }
            Assert.True(issueFound && formatCorrect);
            PlayerSettings.bakeCollisionMeshes = bakeCollisionMeshes;
        }
    }
}
