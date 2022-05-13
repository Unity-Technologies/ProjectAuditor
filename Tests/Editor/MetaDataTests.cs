using System.Linq;
using NUnit.Framework;
using Unity.ProjectAuditor.Editor;
using Unity.ProjectAuditor.Editor.Modules;
using UnityEngine;

namespace Unity.ProjectAuditor.EditorTests
{
    class MetaDataTests
    {
        [Test]
        public void MetaData_IsReported()
        {
            var matchingIssue = Utility.Analyze(IssueCategory.MetaData, issue => issue.description.Equals("Unity Version")).FirstOrDefault();

            Assert.NotNull(matchingIssue);
            Assert.AreEqual(Application.unityVersion, matchingIssue.GetCustomProperty(MetaDataProperty.Value));
        }
    }
}
