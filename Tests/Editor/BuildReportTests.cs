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
        public void BuildReport_IsSupported()
        {
            var projectAuditor = new Unity.ProjectAuditor.Editor.ProjectAuditor();
            var module = projectAuditor.GetModule<BuildReportModule>();
            var isSupported = module.IsSupported();
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
        public void BuildReport_Files_AreReported()
        {
            var issues = Utility.AnalyzeBuild().GetIssues(IssueCategory.BuildFile);
            var matchingIssue = issues.FirstOrDefault(i => i.relativePath.Equals(m_TempAsset.relativePath));

            Assert.NotNull(matchingIssue);
            Assert.True(matchingIssue.description.Equals(Path.GetFileNameWithoutExtension(m_TempAsset.relativePath)));
            Assert.That(matchingIssue.GetNumCustomProperties(), Is.EqualTo((int)BuildReportFileProperty.Num));
            Assert.True(matchingIssue.GetCustomProperty(BuildReportFileProperty.BuildFile).Equals("resources.assets"));
            Assert.That(matchingIssue.GetCustomPropertyAsInt(BuildReportFileProperty.Size), Is.Positive);
            Assert.True(matchingIssue.GetCustomProperty(BuildReportFileProperty.Type).Equals(typeof(Material).ToString()));
        }

        [Test]
#if !UNITY_2019_4_OR_NEWER
        [Ignore("Not Supported in this version of Unity")]
#endif
        public void BuildReport_Steps_AreReported()
        {
            var issues = Utility.AnalyzeBuild().GetIssues(IssueCategory.BuildStep);
            var step = issues.FirstOrDefault(i => i.description.Equals("Build player"));
            Assert.NotNull(step);
            Assert.That(step.depth, Is.EqualTo(0));

            step = issues.FirstOrDefault(i => i.description.Equals("Compile scripts"));
            Assert.NotNull(step, "\"Compile scripts\" string not found");
#if UNITY_2021_1_OR_NEWER
            Assert.That(step.depth, Is.EqualTo(3));
#else
            Assert.That(step.depth, Is.EqualTo(1));
#endif

            step = issues.FirstOrDefault(i => i.description.Equals("Postprocess built player"));
            Assert.NotNull(step, "\"Postprocess built player\" string not found");
            Assert.That(step.depth, Is.EqualTo(1));
        }
    }
}
