using System.Linq;
using NUnit.Framework;
using Unity.ProjectAuditor.Editor;
using Unity.ProjectAuditor.Editor.Modules;

namespace Unity.ProjectAuditor.EditorTests
{
    internal class EditorCodeAnalysisTests
    {
        readonly Unity.ProjectAuditor.Editor.ProjectAuditor m_ProjectAuditor = new Unity.ProjectAuditor.Editor.ProjectAuditor();
        Report m_Report;

        [OneTimeSetUp]
        public void EditorCodeAnalysisSetup()
        {
            m_Report = m_ProjectAuditor.Audit(new AnalysisParams
            {
                CompilationMode = CompilationMode.Editor,
                Categories = new[] { IssueCategory.Code }
            });
        }

        [Test]
        public void EditorCodeAnalysis_GetAssemblies_IsFound()
        {
            var issues = m_Report.FindByCategory(IssueCategory.Code);
            var codeIssue = issues.FirstOrDefault(i => i.Id.IsValid() &&
                i.Id.GetDescriptor().Type.Equals("System.AppDomain") &&
                i.Id.GetDescriptor().Method.Equals("GetAssemblies") &&
                i.GetCustomProperty(CodeProperty.Assembly).Equals("Unity.ProjectAuditor.Editor"));

            Assert.NotNull(codeIssue);
            Assert.AreEqual("'System.AppDomain.GetAssemblies' usage", codeIssue.Description);
        }

        [Test]
        public void EditorCodeAnalysis_FindAssets_IsFound()
        {
            var issues = m_Report.FindByCategory(IssueCategory.Code);
            var codeIssue = issues.FirstOrDefault(i => i.Id.IsValid() &&
                i.Id.GetDescriptor().Type.Equals("UnityEditor.AssetDatabase") &&
                i.Id.GetDescriptor().Method.Equals("FindAssets") &&
                i.GetCustomProperty(CodeProperty.Assembly).Equals("Unity.ProjectAuditor.Editor"));

            Assert.NotNull(codeIssue);
            Assert.AreEqual("'UnityEditor.AssetDatabase.FindAssets' usage", codeIssue.Description);
            Assert.AreEqual("PAC0232", codeIssue.Id.ToString());
        }
    }
}
