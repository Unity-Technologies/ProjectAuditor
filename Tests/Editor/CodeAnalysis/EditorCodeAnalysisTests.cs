using System.Linq;
using NUnit.Framework;
using Unity.ProjectAuditor.Editor;
using Unity.ProjectAuditor.Editor.AssemblyUtils;
using Unity.ProjectAuditor.Editor.Modules;
using UnityEngine;

namespace Unity.ProjectAuditor.EditorTests
{
    public class EditorCodeAnalysisTests
    {
        [Test]
        public void EditorCodeAnalysis_Issue_IsFound()
        {
            var config = ScriptableObject.CreateInstance<ProjectAuditorConfig>();
            config.CompilationMode = CompilationMode.Editor;

            var projectAuditor = new Unity.ProjectAuditor.Editor.ProjectAuditor(config);
            var projectReport = projectAuditor.Audit();

            var issues = projectReport.GetIssues(IssueCategory.Code);
            var codeIssue = issues.FirstOrDefault(i => i.descriptor.type.Equals("System.AppDomain") && i.descriptor.method.Equals("GetAssemblies") && i.GetCustomProperty(CodeProperty.Assembly).Equals("Unity.ProjectAuditor.Editor"));

            Assert.NotNull(codeIssue);
            Assert.AreEqual("'System.AppDomain.GetAssemblies' usage", codeIssue.description);
        }
    }
}
