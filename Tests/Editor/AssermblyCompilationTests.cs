using System.Linq;
using NUnit.Framework;
using Unity.ProjectAuditor.Editor;
using UnityEngine;

namespace UnityEditor.ProjectAuditor.EditorTests
{
    public class AssermblyCompilationTests
    {
        ScriptResource m_ScriptResource;

        [OneTimeSetUp]
        public void SetUp()
        {
            m_ScriptResource = new ScriptResource("MyClass.cs", @"
using UnityEngine;
class MyClass
{
    void Dummy()
    {
#if UNITY_EDITOR
        Debug.Log(Camera.main.name);
#endif
    }
}
");
        }

        [OneTimeTearDown]
        public void TearDown()
        {
            m_ScriptResource.Delete();
        }

        [Test]
        public void EditorCodeIssueIsNotReported()
        {
            var projectAuditor = new Unity.ProjectAuditor.Editor.ProjectAuditor();

            Assert.False(projectAuditor.config.AnalyzeEditorCode);

            var projectReport = projectAuditor.Audit();
            var issues = projectReport.GetIssues(IssueCategory.Code);
            var codeIssue = issues.FirstOrDefault(i => i.relativePath.Equals(m_ScriptResource.relativePath));

            Assert.Null(codeIssue);
        }

        [Test]
        public void EditorCodeIssueIsReported()
        {
            var config = ScriptableObject.CreateInstance<ProjectAuditorConfig>();
            config.AnalyzeEditorCode = true;

            var projectAuditor = new Unity.ProjectAuditor.Editor.ProjectAuditor(config);
            var projectReport = projectAuditor.Audit();

            var issues = projectReport.GetIssues(IssueCategory.Code);

            var codeIssue = issues.FirstOrDefault(i => i.relativePath.Equals(m_ScriptResource.relativePath));
            Assert.NotNull(codeIssue);
        }
    }
}
