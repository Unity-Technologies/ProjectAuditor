using System;
using System.Linq;
using NUnit.Framework;
using Unity.ProjectAuditor.Editor;

namespace UnityEditor.ProjectAuditor.EditorTests
{
    class AreaNameTests
    {
        [Test]
        public void AreaNameIsValid()
        {
            // disabling stripUnusedMeshComponents will be reported as an issue
            PlayerSettings.stripUnusedMeshComponents = false;

            var issues = Utility.Analyze(IssueCategory.ProjectSetting);

            var strippingIssue = issues.FirstOrDefault(i => i.descriptor.method.Equals("stripUnusedMeshComponents"));
            Assert.NotNull(strippingIssue);
            Assert.True(strippingIssue.descriptor.area.Contains(Area.BuildSize.ToString()));
            Assert.True(strippingIssue.descriptor.area.Contains(Area.LoadTimes.ToString()));
            Assert.True(strippingIssue.descriptor.area.Contains(Area.GPU.ToString()));
            Assert.True(strippingIssue.descriptor.area.Equals("BuildSize|LoadTimes|GPU"));
        }
    }
}
