using System;
using System.Linq;
using NUnit.Framework;
using Unity.ProjectAuditor.Editor;
using Unity.ProjectAuditor.Editor.Utils;
using UnityEditor;

namespace Unity.ProjectAuditor.EditorTests
{
    class AreaTests
    {
        [Test]
        public void Area_Name_IsValid()
        {
            // disabling stripUnusedMeshComponents will be reported as an issue
            var stripUnusedMeshComponents = PlayerSettings.stripUnusedMeshComponents;
            PlayerSettings.stripUnusedMeshComponents = false;

            var issues = Utility.Analyze(IssueCategory.ProjectSetting, i => i.descriptor.method.Equals("stripUnusedMeshComponents"));

            var issue = issues.FirstOrDefault();
            Assert.NotNull(issue);

            var areas = issue.descriptor.GetAreas();
            Assert.AreEqual(3, areas.Length);
            Assert.Contains(Area.BuildSize, areas);
            Assert.Contains(Area.GPU, areas);
            Assert.Contains(Area.LoadTime, areas);

            // restore stripUnusedMeshComponents
            PlayerSettings.stripUnusedMeshComponents = stripUnusedMeshComponents;
        }
    }
}
