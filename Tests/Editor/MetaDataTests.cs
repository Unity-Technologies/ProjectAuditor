using System.Linq;
using NUnit.Framework;
using Unity.ProjectAuditor.Editor;
using UnityEngine;

namespace UnityEditor.ProjectAuditor.EditorTests
{
    public class MetaDataTests
    {
        [Test]
        public void MetaDataIsReported()
        {
            var projectAuditor = new Unity.ProjectAuditor.Editor.ProjectAuditor();

            var projectReport = projectAuditor.Audit();
            var issues = projectReport.GetIssues(IssueCategory.MetaData);
            var matchingIssue = issues.FirstOrDefault(i => i.description.Equals("Unity Version"));

            Assert.NotNull(matchingIssue);
            Assert.True(matchingIssue.GetCustomProperty(0).Equals(Application.unityVersion));
        }
    }
}
