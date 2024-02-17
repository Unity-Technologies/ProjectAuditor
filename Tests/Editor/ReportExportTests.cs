using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Unity.ProjectAuditor.Editor;
using Unity.ProjectAuditor.Editor.AssemblyUtils;
using Unity.ProjectAuditor.Editor.Core;
using Unity.ProjectAuditor.Editor.Tests.Common;

namespace Unity.ProjectAuditor.EditorTests
{
    class ReportExportTests : TestFixtureBase
    {
#pragma warning disable 0414
        TestAsset m_TestAsset;
#pragma warning restore 0414

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            m_TestAsset = new TestAsset("MyClass.cs", @"
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
        public void Report_CodeIssues_AreExportedAndFormatted_CSV()
        {
            var category = IssueCategory.Code;
            var path = string.Format("project-auditor-report-{0}.csv", category.ToString()).ToLower();
            AnalyzeAndExport(category, path, "csv");
            var issueExported = false;
            using (var file = new StreamReader(path))
            {
                var line = file.ReadLine();
                Assert.AreEqual("Issue,Severity,Areas,Filename,Assembly,Descriptor", line);

                var expectedIssueLine = $"\"'UnityEngine.Camera.allCameras' usage\",\"{Severity.Major}\",\"{Areas.Memory}\",\"MyClass.cs:7\",\"Assembly-CSharp\",\"UnityEngine.Camera.allCameras\"";
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
        public void Report_CodeIssues_AreExportedAndFormatted_HTML()
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
                Assert.AreEqual($"<th>Severity</th>", line);
                line = file.ReadLine();
                Assert.AreEqual("<th>Areas</th>", line);
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
                        if (line.Equals($"<td>{Severity.Major}</td>"))
                        {
                            index++;
                        }
                        line = file.ReadLine();
                        if (line.Equals($"<td>{Areas.Memory}</td>"))
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
        public void Report_CodeIssues_AreFilteredAndExported_CSV()
        {
            var category = IssueCategory.Code;
            var path = string.Format("project-auditor-report-{0}.csv", category.ToString()).ToLower();
            AnalyzeAndExport(category, path, "csv", issue =>
            {
                return issue.Description.StartsWith("Conversion");
            });
            var issueExported = false;
            using (var file = new StreamReader(path))
            {
                var line = file.ReadLine();
                Assert.AreEqual("Issue,Severity,Areas,Filename,Assembly,Descriptor", line);

                var expectedIssueLine = $"\"Conversion from value type 'Int32' to ref type\",\"{Severity.Major}\",\"{Areas.Memory}\",\"MyClass.cs:7\",\"Assembly-CSharp\",\"Boxing Allocation\"";
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
        public void Report_CodeIssues_AreFilteredAndExported_HTML()
        {
            var category = IssueCategory.Code;
            var path = string.Format("project-auditor-report-{0}.html", category.ToString()).ToLower();
            AnalyzeAndExport(category, path, "html", issue =>
            {
                return issue.Description.StartsWith("Conversion");
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
                line = file.ReadLine();     //should be "<th>Severity</th>"
                line = file.ReadLine();     //should be "<th>Areas</th>"
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
                        if (line.Equals($"<td>{Severity.Major}</td>"))
                        {
                            index++;
                        }
                        line = file.ReadLine();
                        if (line.Equals($"<td>{Areas.Memory}</td>"))
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
        public void Report_SettingsIssues_AreExportedAndFormatted_CSV()
        {
            var category = IssueCategory.ProjectSetting;
            var path = string.Format("project-auditor-report-{0}.csv", category.ToString()).ToLower();
            var issues = AnalyzeAndExport(category,  path, "csv", i => i.Id.Equals("PAS0007"));

            Assert.AreEqual(1, issues.Count);

            var issue = issues.First();
            var descriptor = issue.Id.GetDescriptor();
            var expectedIssueLine = $"\"{issue.Description}\",\"{Severity.Moderate}\",\"{descriptor.GetAreasSummary()}\",\"{issue.Filename}\",\"{descriptor.GetPlatformsSummary()}\"";

            var issueExported = false;
            using (var file = new StreamReader(path))
            {
                var line = file.ReadLine();
                Assert.AreEqual("Issue,Severity,Areas,System,Platform", line, "Header was: " + line);
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
        public void Report_SettingsIssues_AreExportedAndFormatted_HTML()
        {
            var category = IssueCategory.ProjectSetting;
            var path = string.Format("project-auditor-report-{0}.html", category.ToString().ToLower());
            var issues = AnalyzeAndExport(category,  path, "html", i => i.Id.Equals("PAS0007"));

            Assert.AreEqual(1, issues.Count);

            var issue = issues.First();
            var descriptor = issue.Id.GetDescriptor();
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
                Assert.AreEqual("<th>Severity</th>", line);
                line = file.ReadLine();
                Assert.AreEqual("<th>Areas</th>", line);
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
                        if (line.Equals($"<td>{issue.Description}</td>"))
                        {
                            index++;
                        }
                        line = file.ReadLine();
                        if (line.Equals($"<td>{issue.GetProperty(PropertyType.Severity)}</td>"))
                        {
                            index++;
                        }
                        line = file.ReadLine();
                        if (line.Equals($"<td>{descriptor.GetAreasSummary()}</td>"))
                        {
                            index++;
                        }
                        line = file.ReadLine();
                        if (line.Equals($"<td>{issue.Filename}</td>"))
                        {
                            index++;
                        }
                        line = file.ReadLine();
                        if (line.Equals($"<td>{descriptor.GetPlatformsSummary()}</td>"))
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
        }

        IReadOnlyCollection<ReportItem> AnalyzeAndExport(IssueCategory category, string path, string format, Func<ReportItem, bool> predicate = null)
        {
            var projectAuditor = new Unity.ProjectAuditor.Editor.ProjectAuditor();
            var report = projectAuditor.Audit(new AnalysisParams
            {
                AssemblyNames = new[] { AssemblyInfo.DefaultAssemblyName },
                Categories = new[] { category },
                CompilationMode = CompilationMode.Player
            });

            switch (format)
            {
                case "csv":
                    using (var exporter = new CsvExporter(report))
                    {
                        exporter.Export(path, category, predicate);
                    }
                    break;
                case "html":
                    using (var exporter = new HtmlExporter(report))
                    {
                        exporter.Export(path, category, predicate);
                    }
                    break;
                default:
                    break;
            }

            Assert.True(File.Exists(path));

            return report.FindByCategory(category).Where(i => (predicate != null) ? predicate(i) : true).ToArray();
        }
    }
}
