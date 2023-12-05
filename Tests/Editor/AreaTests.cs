using System.Linq;
using NUnit.Framework;
using Unity.ProjectAuditor.Editor;
using Unity.ProjectAuditor.Editor.Diagnostic;
using Unity.ProjectAuditor.Editor.Tests.Common;
using UnityEditor;

namespace Unity.ProjectAuditor.EditorTests
{
    class AreaTests : TestFixtureBase
    {
        [Test]
        public void Area_Name_IsValid()
        {
            // disabling stripUnusedMeshComponents will be reported as an issue
            var stripUnusedMeshComponents = PlayerSettings.stripUnusedMeshComponents;
            PlayerSettings.stripUnusedMeshComponents = false;

            var issues = Analyze(IssueCategory.ProjectSetting, i =>
                i.Id.GetDescriptor().Method.Equals("stripUnusedMeshComponents"));

            var issue = issues.FirstOrDefault();
            Assert.NotNull(issue);

            var descriptor = issue.Id.GetDescriptor();
            Assert.AreEqual((Areas.BuildSize | Areas.GPU | Areas.LoadTime), descriptor.Areas);

            // restore stripUnusedMeshComponents
            PlayerSettings.stripUnusedMeshComponents = stripUnusedMeshComponents;
        }
    }
}
