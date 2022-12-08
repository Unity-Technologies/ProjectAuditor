using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Unity.ProjectAuditor.Editor;
using Unity.ProjectAuditor.Editor.AssemblyUtils;
using Unity.ProjectAuditor.Editor.Core;
using Unity.ProjectAuditor.Editor.Diagnostic;
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
            Assert.IsEmpty(projectReport.FindByCategory(IssueCategory.Code));
            Assert.IsEmpty(projectReport.FindByCategory(IssueCategory.ProjectSetting));
        }

        [Test]
        public void ProjectReport_Issue_IsAdded()
        {
            var projectReport = new ProjectReport();
            var p = new Descriptor
                (
                "TD2001",
                "test",
                Area.CPU,
                "this is not actually a problem",
                "do nothing"
                );

            projectReport.AddIssues(new[] { new ProjectIssue
                                            (
                                                IssueCategory.Code,
                                                p,
                                                "dummy issue"
                                            ) }
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

            return projectReport.FindByCategory(category).Where(i => (predicate != null) ? predicate(i) : true).ToArray();
        }

        [Test]
        public void ProjectReport_CodeIssues_AreExportedAndFormatted_CSV()
        {
            var category = IssueCategory.Code;
            var path = string.Format("project-auditor-report-{0}.csv", category.ToString()).ToLower();
            AnalyzeAndExport(category, path, "csv");
            var issueExported = false;
            using (var file = new StreamReader(path))
            {
                var line = file.ReadLine();
                Assert.AreEqual("Issue,Critical,Area,Filename,Assembly,Descriptor", line);

                var expectedIssueLine = "\"'UnityEngine.Camera.allCameras' usage\",\"True\",\"Memory\",\"MyClass.cs:7\",\"Assembly-CSharp\",\"UnityEngine.Camera.allCameras\"";
                while (file.Peek() >= 0)
                {
                    line = file.ReadLine();
                    if (line.Equals(expectedIssueLine))
                        issueExported = true;
                }
            }

            Assert.True(issueExported);
        }

        [Test]
        public void ProjectReport_CodeIssues_AreExportedAndFormatted_HTML()
        {
            var category = IssueCategory.Code;
            var path = string.Format("project-auditor-report-{0}.html", category.ToString()).ToLower();
            AnalyzeAndExport(category, path, "html");

            var issueExported = false;
            var formatCorrect = false;
            using (var file = new StreamReader(path))
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
                Assert.AreEqual("<th>Critical</th>", line);
                line = file.ReadLine();
                Assert.AreEqual("<th>Area</th>", line);
                line = file.ReadLine();
                Assert.AreEqual("<th>Filename</th>", line);
                line = file.ReadLine();
                Assert.AreEqual("<th>Assembly</th>", line);
                line = file.ReadLine();
                Assert.AreEqual("<th>Descriptor</th>", line);
                line = file.ReadLine();
                Assert.AreEqual("</tr>", line);

                while (file.Peek() > -1)
                {
                    line = file.ReadLine();
                    if (!line.Equals("</body>"))
                    {
                        int index = 0;
                        if (line.Equals("<tr>"))
                        {
                            index++;
                        }
                        line = file.ReadLine();
                        if (line.Equals($"<td>'UnityEngine.Camera.allCameras' usage</td>"))
                        {
                            index++;
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
                        if (line.Equals($"<td>UnityEngine.Camera.allCameras</td>"))
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
                            issueExported = true;
                        }
                    }
                    else
                    {
                        line = file.ReadLine();
                        if (line.Equals("</html>"))
                        {
                            formatCorrect = true;
                        }
                    }
                }
            }
            Assert.True(issueExported);
            Assert.True(formatCorrect);
        }

        [Test]
        public void ProjectReport_CodeIssues_AreFilteredAndExported_CSV()
        {
            var category = IssueCategory.Code;
            var path = string.Format("project-auditor-report-{0}.csv", category.ToString()).ToLower();
            AnalyzeAndExport(category, path, "csv", issue =>
            {
                return issue.description.StartsWith("Conversion");
            });
            var issueExported = false;
            using (var file = new StreamReader(path))
            {
                var line = file.ReadLine();
                Assert.AreEqual("Issue,Critical,Area,Filename,Assembly,Descriptor", line);

                var expectedIssueLine = "\"Conversion from value type 'Int32' to ref type\",\"True\",\"Memory\",\"MyClass.cs:7\",\"Assembly-CSharp\",\"Boxing Allocation\"";
                while (file.Peek() >= 0)
                {
                    line = file.ReadLine();
                    if (line.Equals(expectedIssueLine))
                        issueExported = true;
                }
            }

            Assert.True(issueExported);
        }

        [Test]
        public void ProjectReport_CodeIssues_AreFilteredAndExported_HTML()
        {
            var category = IssueCategory.Code;
            var path = string.Format("project-auditor-report-{0}.html", category.ToString()).ToLower();
            AnalyzeAndExport(category, path, "html", issue =>
            {
                return issue.description.StartsWith("Conversion");
            });
            var issueExported = false;
            var filterCorrect = true;
            using (var file = new StreamReader(path))
            {
                var line = file.ReadLine(); //should be "<html>"
                line = file.ReadLine();     //should be "<body>"
                line = file.ReadLine();     //should be "<<table width='50%' cellpadding='10' style='margin-top:10px' cellspacing='3' border='1' rules='all'>"
                line = file.ReadLine();     //should be "<tr>"
                line = file.ReadLine();     //should be "<th>Issue</th>"
                line = file.ReadLine();     //should be "<th>Critical</th>"
                line = file.ReadLine();     //should be "<th>Area</th>"
                line = file.ReadLine();     //should be "<th>Filename</th>"
                line = file.ReadLine();     //should be "<th>Assembly</th>"
                line = file.ReadLine();     //should be "<th>Descriptor</th>"
                line = file.ReadLine();     //should be "</tr>"

                while (file.Peek() > -1)
                {
                    line = file.ReadLine();
                    if (!line.Equals("</body>"))
                    {
                        int index = 0;
                        if (line.Equals("<tr>"))
                        {
                            index++;
                        }
                        line = file.ReadLine();
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
                            issueExported = true;
                        }
                    }
                    else
                    {
                        line = file.ReadLine();
                    }
                }
                Assert.True(issueExported);
                Assert.True(filterCorrect);
            }
        }

        [Test]
        public void ProjectReport_SettingsIssues_AreExportedAndFormatted_CSV()
        {
            var bakeCollisionMeshes = PlayerSettings.bakeCollisionMeshes;
            PlayerSettings.bakeCollisionMeshes = false;

            var category = IssueCategory.ProjectSetting;
            var path = string.Format("project-auditor-report-{0}.csv", category.ToString()).ToLower();
            var issues = AnalyzeAndExport(category,  path, "csv", i => i.descriptor.id.Equals("PAS0007"));

            Assert.AreEqual(1, issues.Count);

            var issue = issues.First();
            var expectedIssueLine = $"\"{issue.description}\",\"False\",\"{issue.descriptor.GetAreasSummary()}\",\"{issue.filename}\",\"{issue.descriptor.GetPlatformsSummary()}\"";

            var issueExported = false;
            using (var file = new StreamReader(path))
            {
                var line = file.ReadLine();
                Assert.AreEqual("Issue,Critical,Area,System,Platform", line, "Header was: " + line);
                while (file.Peek() >= 0)
                {
                    line = file.ReadLine();
                    if (line.Equals(expectedIssueLine))
                        issueExported = true;
                }
            }

            Assert.True(issueExported);

            PlayerSettings.bakeCollisionMeshes = bakeCollisionMeshes;
        }

        [Test]
        public void ProjectReport_SettingsIssues_AreExportedAndFormatted_HTML()
        {
            var bakeCollisionMeshes = PlayerSettings.bakeCollisionMeshes;
            PlayerSettings.bakeCollisionMeshes = false;

            var category = IssueCategory.ProjectSetting;
            var path = string.Format("project-auditor-report-{0}.html", category.ToString().ToLower());
            var issues = AnalyzeAndExport(category,  path, "html", i => i.descriptor.id.Equals("PAS0007"));

            Assert.AreEqual(1, issues.Count);

            var issue = issues.First();
            var issueExported = false;
            var formatCorrect = false;
            using (var file = new StreamReader(path))
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
                Assert.AreEqual("<th>Critical</th>", line);
                line = file.ReadLine();
                Assert.AreEqual("<th>Area</th>", line);
                line = file.ReadLine();
                Assert.AreEqual("<th>System</th>", line);
                line = file.ReadLine();
                Assert.AreEqual("<th>Platform</th>", line);
                line = file.ReadLine();
                Assert.AreEqual("</tr>", line);

                while (file.Peek() > -1)
                {
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
                        if (line.Equals($"<td>{issue.GetProperty(PropertyType.Priority)}</td>"))
                        {
                            index++;
                        }
                        line = file.ReadLine();
                        if (line.Equals($"<td>{issue.descriptor.GetAreasSummary()}</td>"))
                        {
                            index++;
                        }
                        line = file.ReadLine();
                        if (line.Equals($"<td>{issue.filename}</td>"))
                        {
                            index++;
                        }
                        line = file.ReadLine();
                        if (line.Equals($"<td>{issue.descriptor.GetPlatformsSummary()}</td>"))
                        {
                            index++;
                        }
                        line = file.ReadLine();
                        if (line.Equals("</tr>"))
                        {
                            index++;
                        }
                        if (index == 7)
                        {
                            issueExported = true;
                        }
                    }
                    else
                    {
                        line = file.ReadLine();
                        if (line.Equals("</html>"))
                        {
                            formatCorrect = true;
                        }
                    }
                }
            }
            Assert.True(issueExported);
            Assert.True(formatCorrect);

            PlayerSettings.bakeCollisionMeshes = bakeCollisionMeshes;
        }
    }
}
