using System.Linq;
using NUnit.Framework;
using Unity.ProjectAuditor.Editor;
using Unity.ProjectAuditor.Editor.Modules;
using Unity.ProjectAuditor.TestUtils;
using UnityEngine;

namespace Unity.ProjectAuditor.EditorTests
{
    class MetaDataTests : TestFixtureBase
    {
        [Test]
        public void MetaData_IsReported()
        {
            var matchingIssue = Analyze(IssueCategory.MetaData, issue => issue.description.Equals("Unity Version")).FirstOrDefault();

            Assert.NotNull(matchingIssue);
            Assert.AreEqual(Application.unityVersion, matchingIssue.GetCustomProperty(MetaDataProperty.Value));
        }
    }
}
