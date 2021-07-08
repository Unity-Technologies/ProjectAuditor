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
            Assert.Contains(Area.GPU, issue.descriptor.areasAsEnums);
            Assert.Contains(Area.LoadTime, issue.descriptor.areasAsEnums);

            // restore stripUnusedMeshComponents
            PlayerSettings.stripUnusedMeshComponents = stripUnusedMeshComponents;
        }
    }
}
