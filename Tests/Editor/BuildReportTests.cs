using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Unity.ProjectAuditor.Editor;
using Unity.ProjectAuditor.Editor.Build;
using Unity.ProjectAuditor.Editor.Modules;
using Unity.ProjectAuditor.Editor.Tests.Common;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace Unity.ProjectAuditor.EditorTests
{
    class BuildReportTests : TestFixtureBase
    {
        TestAsset m_TestAsset;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            var material = new Material(Shader.Find("UI/Default"));
            m_TestAsset = TestAsset.Save(material, "Resources/Shiny.mat");
        }

        [Test]
        public void BuildReport_Files_AreReported()
        {
            var issues = AnalyzeBuild(IssueCategory.BuildFile, i => i.RelativePath.Equals(m_TestAsset.RelativePath));
            var matchingIssue = issues.FirstOrDefault();

            Assert.NotNull(matchingIssue);

            var buildFile = matchingIssue.GetCustomProperty(BuildReportFileProperty.BuildFile);
            var buildReport = BuildReportModule.BuildReportProvider.GetBuildReport(m_Platform);

            Assert.NotNull(buildReport);

            var reportedCorrectAssetBuildFile = buildReport.packedAssets.Any(p => p.shortPath == buildFile && p.contents.Any(c => c.sourceAssetPath == m_TestAsset.RelativePath));

            Assert.AreEqual(Path.GetFileNameWithoutExtension(m_TestAsset.RelativePath), matchingIssue.Description);
            Assert.That(matchingIssue.GetNumCustomProperties(), Is.EqualTo((int)BuildReportFileProperty.Num));
            Assert.True(reportedCorrectAssetBuildFile);
            Assert.AreEqual(typeof(AssetImporter).Name, matchingIssue.GetCustomProperty(BuildReportFileProperty.ImporterType));
            Assert.AreEqual(typeof(Material).Name, matchingIssue.GetCustomProperty(BuildReportFileProperty.RuntimeType));
            Assert.That(matchingIssue.GetCustomPropertyInt32(BuildReportFileProperty.Size), Is.Positive);
        }

        [Test]
        public void BuildReport_Steps_AreReported()
        {
            var issues = AnalyzeBuild(IssueCategory.BuildStep);
            var step = issues.FirstOrDefault(i => i.Description.Equals("Build player"));
            Assert.NotNull(step);
            Assert.That(step.GetCustomPropertyInt32(BuildReportStepProperty.Depth), Is.EqualTo(0));

            step = issues.FirstOrDefault(i => i.Description.Equals("Compile scripts"));
            Assert.NotNull(step, "\"Compile scripts\" string not found");
#if UNITY_2021_1_OR_NEWER
            Assert.That(step.GetCustomPropertyInt32(BuildReportStepProperty.Depth), Is.EqualTo(3));
#else
            Assert.That(step.GetCustomPropertyInt32(BuildReportStepProperty.Depth), Is.EqualTo(1));
#endif

            step = issues.FirstOrDefault(i => i.Description.Equals("Postprocess built player"));
            Assert.NotNull(step, "\"Postprocess built player\" string not found");
            Assert.That(step.GetCustomPropertyInt32(BuildReportStepProperty.Depth), Is.EqualTo(1));
        }
    }
}
