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
            var config = ScriptableObject.CreateInstance<ProjectAuditorConfig>();
            var projectAuditor = new Unity.ProjectAuditor.Editor.ProjectAuditor(config);
            var projectReport = projectAuditor.Audit(new ProjectAuditorParams
            {
                compilationMode = CompilationMode.Editor
            });

            var issues = projectReport.FindByCategory(IssueCategory.Code);
            var codeIssue = issues.FirstOrDefault(i => DescriptorLibrary.TryGetDescriptor(i.Id, out var descriptor) &&
                                                       descriptor.type.Equals("System.AppDomain") &&
                                                       descriptor.method.Equals("GetAssemblies") &&
                                                       i.GetCustomProperty(CodeProperty.Assembly).Equals("Unity.ProjectAuditor.Editor"));

            Assert.NotNull(codeIssue);
            Assert.AreEqual("'System.AppDomain.GetAssemblies' usage", codeIssue.description);
        }

        [Test]
        public void EditorCodeAnalysis_FindAssets_IsFound()
        {
            var config = ScriptableObject.CreateInstance<ProjectAuditorConfig>();
            var projectAuditor = new Unity.ProjectAuditor.Editor.ProjectAuditor(config);
            var projectReport = projectAuditor.Audit(new ProjectAuditorParams
            {
                compilationMode = CompilationMode.Editor
            });

            var issues = projectReport.FindByCategory(IssueCategory.Code);
            var codeIssue = issues.FirstOrDefault(i => DescriptorLibrary.TryGetDescriptor(i.Id, out var descriptor) &&
                                                       descriptor.type.Equals("UnityEditor.AssetDatabase") &&
                                                       descriptor.method.Equals("FindAssets") &&
                                                       i.GetCustomProperty(CodeProperty.Assembly).Equals("Unity.ProjectAuditor.Editor"));

            Assert.NotNull(codeIssue);
            Assert.AreEqual("'UnityEditor.AssetDatabase.FindAssets' usage", codeIssue.description);
            Assert.AreEqual("PAC0232", codeIssue.Id);
        }
    }
}
