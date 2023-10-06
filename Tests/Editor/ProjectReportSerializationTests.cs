using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using NUnit.Framework;
using Unity.ProjectAuditor.Editor;
using Unity.ProjectAuditor.Editor.Tests.Common;

namespace Unity.ProjectAuditor.EditorTests
{
    class ProjectReportSerializationTests : TestFixtureBase
    {
        const string k_ReportPath = "report.json";

        [Test]
        public void ProjectReportSerialization_Report_CanSaveAndLoad()
        {
            var report = m_ProjectAuditor.Audit();

            report.Save(k_ReportPath);

            var loadedReport = ProjectReport.Load(k_ReportPath);

            Assert.AreEqual(report.Version, loadedReport.Version);
            Assert.AreEqual(report.NumTotalIssues, loadedReport.NumTotalIssues);
            Assert.IsTrue(report.IsValid());
            Assert.IsTrue(report.HasCategory(IssueCategory.Code));
            Assert.IsTrue(report.HasCategory(IssueCategory.ProjectSetting));
        }

        [Test]
        public void ProjectReportSerialization_Report_CanSerialize()
        {
            var report = m_ProjectAuditor.Audit();
            report.Save(k_ReportPath);

            var serializedReport = File.ReadAllText(k_ReportPath);

            var reportDict = JsonConvert.DeserializeObject<Dictionary<string, object>>(serializedReport);

            // Check if top-level keys exist.
            Assert.IsTrue(reportDict.ContainsKey("version"));
            Assert.IsTrue(reportDict.ContainsKey("moduleMetadata"));
            Assert.IsTrue(reportDict.ContainsKey("issues"));
            Assert.IsTrue(reportDict.ContainsKey("descriptors"));
        }
    }
}
