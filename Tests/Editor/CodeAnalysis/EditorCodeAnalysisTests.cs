using System.Linq;
using NUnit.Framework;
using Unity.ProjectAuditor.Editor;
using Unity.ProjectAuditor.Editor.AssemblyUtils;
using Unity.ProjectAuditor.Editor.Core;
using Unity.ProjectAuditor.Editor.Diagnostic;
using Unity.ProjectAuditor.Editor.Modules;
using UnityEngine;

namespace Unity.ProjectAuditor.EditorTests
{
    internal class EditorCodeAnalysisTests
    {
        [Test]
        public void EditorCodeAnalysis_GetAssemblies_IsFound()
        {
            var projectAuditor = new Unity.ProjectAuditor.Editor.ProjectAuditor();
            var projectReport = projectAuditor.Audit(new AnalysisParams
            {
                CompilationMode = CompilationMode.Editor,
                Categories = new[] { IssueCategory.Code }
            });

            var issues = projectReport.FindByCategory(IssueCategory.Code);
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
            var projectAuditor = new Unity.ProjectAuditor.Editor.ProjectAuditor();
            var projectReport = projectAuditor.Audit(new AnalysisParams
            {
                CompilationMode = CompilationMode.Editor,
                Categories = new[] { IssueCategory.Code }
            });

            var issues = projectReport.FindByCategory(IssueCategory.Code);
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
