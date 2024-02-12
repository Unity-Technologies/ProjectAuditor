using System.Linq;
using NUnit.Framework;
using Unity.ProjectAuditor.Editor;
using Unity.ProjectAuditor.Editor.Tests.Common;
using UnityEditor;

namespace Unity.ProjectAuditor.EditorTests
{
    class AreaTests : TestFixtureBase
    {
        [Test]
        public void Area_Name_IsValid()
        {
            var issues = Analyze(IssueCategory.ProjectSetting, i =>
                i.Id.GetDescriptor().Method.Equals(nameof(PlayerSettings.bakeCollisionMeshes)));

            var issue = issues.FirstOrDefault();
            Assert.NotNull(issue);

            var descriptor = issue.Id.GetDescriptor();
            Assert.AreEqual((Areas.BuildSize | Areas.LoadTime), descriptor.Areas);
        }
    }
}
