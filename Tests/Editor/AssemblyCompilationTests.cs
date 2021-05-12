using System;
using System.Linq;
using NUnit.Framework;
using Unity.ProjectAuditor.Editor;
using Unity.ProjectAuditor.Editor.Utils;
using UnityEngine;

namespace UnityEditor.ProjectAuditor.EditorTests
{
    class AssemblyCompilationTests
    {
        TempAsset m_TempAsset;

        [OneTimeSetUp]
        public void SetUp()
        {
            m_TempAsset = new TempAsset("MyClass.cs", @"
using UnityEngine;
class MyClass
{
    void Dummy()
    {
#if UNITY_EDITOR
        Debug.Log(Camera.allCameras.Length);
#endif
    }
}
");
        }

        [OneTimeTearDown]
        public void TearDown()
        {
            TempAsset.Cleanup();
        }

        [Test]
        public void DefaultAssemblyIsCompiled()
        {
            using (var compilationHelper = new AssemblyCompilationPipeline())
            {
                var assemblyInfos = compilationHelper.Compile();

                Assert.Positive(assemblyInfos.Count());
                Assert.NotNull(assemblyInfos.FirstOrDefault(info => info.name.Equals(AssemblyInfo.DefaultAssemblyName)));
            }
        }

        [Test]
        public void EditorCodeIssueIsNotReported()
        {
            var config = ScriptableObject.CreateInstance<ProjectAuditorConfig>();
            config.AnalyzeEditorCode = false;

            var projectAuditor = new Unity.ProjectAuditor.Editor.ProjectAuditor(config);

            var projectReport = projectAuditor.Audit();
            var issues = projectReport.GetIssues(IssueCategory.Code);
            var codeIssue = issues.FirstOrDefault(i => i.relativePath.Equals(m_TempAsset.relativePath));

            Assert.Null(codeIssue);
        }

        [Test]
        [Ignore("Known failure because the script is not recompiled by the editor")]
        public void EditorCodeIssueIsReported()
        {
            var config = ScriptableObject.CreateInstance<ProjectAuditorConfig>();
            config.AnalyzeEditorCode = true;

            var projectAuditor = new Unity.ProjectAuditor.Editor.ProjectAuditor(config);
            var projectReport = projectAuditor.Audit();

            var issues = projectReport.GetIssues(IssueCategory.Code);
            var codeIssue = issues.FirstOrDefault(i => i.relativePath.Equals(m_TempAsset.relativePath));

            Assert.NotNull(codeIssue);
        }
    }
}
