using NUnit.Framework;
using Unity.ProjectAuditor.Editor;
using Unity.ProjectAuditor.TestUtils;

namespace Unity.ProjectAuditor.EditorTests
{
    class SaveLoadTests : TestFixtureBase
    {
        const string k_ReportPath = "report.json";

        [Test]
        public void SaveLoad_Report_CanSaveAndLoad()
        {
            var projectReport = m_ProjectAuditor.Audit();
            projectReport.Save(k_ReportPath);

            var loadedReport = ProjectReport.Load(k_ReportPath);

            Assert.AreEqual(projectReport.NumTotalIssues, loadedReport.NumTotalIssues);
        }
    }
}
