using System.Linq;
using NUnit.Framework;
using Unity.ProjectAuditor.Editor;

namespace UnityEditor.ProjectAuditor.EditorTests
{
    public class AreaNameTests
    {
        [Test]
        public void AreaNameIsValid()
        {
            var projectAuditor = new Unity.ProjectAuditor.Editor.ProjectAuditor();

            // disabling stripUnusedMeshComponents will be reported as an issue
            PlayerSettings.stripUnusedMeshComponents = false;

            var projectReport = projectAuditor.Audit();
            var issues = projectReport.GetIssues(IssueCategory.ProjectSettings);

            var strippingIssue = issues.FirstOrDefault(i => i.descriptor.method.Equals("stripUnusedMeshComponents"));
            Assert.NotNull(strippingIssue);
            Assert.True(strippingIssue.descriptor.area.Contains(Area.BuildSize.ToString()));
            Assert.True(strippingIssue.descriptor.area.Contains(Area.LoadTimes.ToString()));
            Assert.True(strippingIssue.descriptor.area.Contains(Area.GPU.ToString()));
            Assert.True(strippingIssue.descriptor.area.Equals("BuildSize|LoadTimes|GPU"));
        }
    }
}
