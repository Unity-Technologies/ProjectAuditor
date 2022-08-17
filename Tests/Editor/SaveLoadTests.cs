using NUnit.Framework;
using Unity.ProjectAuditor.Editor;

namespace Unity.ProjectAuditor.EditorTests
{
    class SaveLoadTests
    {
        const string k_ReportPath = "report.json";

        [Test]
        public void SaveLoad_Report_CanSaveAndLoad()
        {
            var projectAuditor = new Unity.ProjectAuditor.Editor.ProjectAuditor();

            var projectReport = projectAuditor.Audit();
            projectReport.Save(k_ReportPath);

            var loadedReport = ProjectReport.Load(k_ReportPath);

            Assert.AreEqual(projectReport.NumTotalIssues, loadedReport.NumTotalIssues);
        }
    }
}
