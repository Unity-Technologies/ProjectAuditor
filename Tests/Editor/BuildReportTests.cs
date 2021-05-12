using System.IO;
using System.Linq;
using NUnit.Framework;
using Unity.ProjectAuditor.Editor;
using Unity.ProjectAuditor.Editor.Auditors;
using UnityEngine;

namespace UnityEditor.ProjectAuditor.EditorTests
{
    class BuildReportTests
    {
        private TempAsset m_TempAsset;

        [OneTimeSetUp]
        public void SetUp()
        {
            var material = new Material(Shader.Find("UI/Default"));
            m_TempAsset = TempAsset.Save(material, "Resources/Shiny.mat");
        }

        [OneTimeTearDown]
        public void TearDown()
        {
            TempAsset.Cleanup();
        }

        [Test]
        public void BuildReportIsSupported()
        {
            var projectAuditor = new Unity.ProjectAuditor.Editor.ProjectAuditor();
            var auditor = projectAuditor.GetAuditor<BuildAuditor>();
            var isSupported = auditor.IsSupported();
#if UNITY_2019_4_OR_NEWER
            Assert.True(isSupported);
#else
            Assert.False(isSupported);
#endif
        }

        [Test]
#if !UNITY_2019_4_OR_NEWER
        [Ignore("Not Supported in this version of Unity")]
#endif
        public void BuildReportIsFound()
        {
            var issues = Utility.AnalyzeBuild().GetIssues(IssueCategory.BuildFiles);
            var matchingIssue = issues.FirstOrDefault(i => i.relativePath.Equals(m_TempAsset.relativePath));

            Assert.NotNull(matchingIssue);
            Assert.True(matchingIssue.description.Equals(Path.GetFileNameWithoutExtension(m_TempAsset.relativePath)));
            Assert.That(matchingIssue.GetNumCustomProperties(), Is.EqualTo((int)BuildProperty.Num));
            Assert.True(matchingIssue.GetCustomProperty((int)BuildProperty.BuildFile).Equals("resources.assets"));
            Assert.That(matchingIssue.GetCustomPropertyAsInt((int)BuildProperty.Size), Is.Positive);
        }
    }
}
