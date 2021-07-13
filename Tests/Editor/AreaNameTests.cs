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
            var stripUnusedMeshComponents = PlayerSettings.stripUnusedMeshComponents = false;
            PlayerSettings.stripUnusedMeshComponents = false;

            var issues = Utility.Analyze(IssueCategory.ProjectSetting);

            var issue = issues.FirstOrDefault(i => i.descriptor.method.Equals("stripUnusedMeshComponents"));
            Assert.NotNull(issue);
            Assert.Contains(Area.BuildSize, issue.descriptor.areasAsEnums);
            Assert.True(strippingIssue.descriptor.area.Contains(Area.LoadTimes.ToString()));
            Assert.Contains(Area.GPU, issue.descriptor.areasAsEnums);
            Assert.True(strippingIssue.descriptor.area.Equals("BuildSize|LoadTimes|GPU"));

            // restore stripUnusedMeshComponents
            PlayerSettings.stripUnusedMeshComponents = stripUnusedMeshComponents;
        }
    }
}
