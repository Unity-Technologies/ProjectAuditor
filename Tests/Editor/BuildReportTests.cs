using System.IO;
using System.Linq;
using NUnit.Framework;
using Unity.ProjectAuditor.Editor;
using Unity.ProjectAuditor.Editor.Modules;
using UnityEditor;
using UnityEngine;

namespace Unity.ProjectAuditor.EditorTests
{
    class BuildReportTests : TestFixtureBase
    {
        TempAsset m_TempAsset;

        [OneTimeSetUp]
        public void SetUp()
        {
            var material = new Material(Shader.Find("UI/Default"));
            m_TempAsset = TempAsset.Save(material, "Resources/Shiny.mat");
        }

        [Test]
        public void BuildReport_IsSupported()
        {
            var projectAuditor = new Unity.ProjectAuditor.Editor.ProjectAuditor();
            var module = projectAuditor.GetModule<BuildReportModule>();
            var isSupported = module.isSupported;
#if BUILD_REPORT_API_SUPPORT
            Assert.True(isSupported);
#else
            Assert.False(isSupported);
#endif
        }

        [Test]
#if !BUILD_REPORT_API_SUPPORT
        [Ignore("Not Supported in this version of Unity")]
#endif
        public void BuildReport_ObjectName_IsCorrect()
        {
            Build();
            var buildReport = BuildReportModule.BuildReportProvider.GetBuildReport();
            var assetPath = AssetDatabase.GetAssetPath(buildReport);
            var assetName = Path.GetFileNameWithoutExtension(assetPath);
            Assert.AreEqual(assetName, buildReport.name);
            Assert.True(assetName.StartsWith("Build_"));
        }

#if BUILD_REPORT_API_SUPPORT
        [Test]
        public void BuildReport_Files_AreReported()
        {
            var issues = AnalyzeBuild(IssueCategory.BuildFile, i => i.relativePath.Equals(m_TempAsset.relativePath));
            var matchingIssue = issues.FirstOrDefault();

            var buildFile = matchingIssue.GetCustomProperty(BuildReportFileProperty.BuildFile);
            var buildReport = BuildReportModule.BuildReportProvider.GetBuildReport();
            var reportedCorrectAssetBuildFile = buildReport.packedAssets.Any(p => p.shortPath == buildFile && p.contents.Any(c => c.sourceAssetPath == m_TempAsset.relativePath));

            Assert.NotNull(matchingIssue);
            Assert.AreEqual(Path.GetFileNameWithoutExtension(m_TempAsset.relativePath), matchingIssue.description);
            Assert.That(matchingIssue.GetNumCustomProperties(), Is.EqualTo((int)BuildReportFileProperty.Num));
            Assert.True(reportedCorrectAssetBuildFile);
            Assert.AreEqual(typeof(AssetImporter).FullName, matchingIssue.GetCustomProperty(BuildReportFileProperty.ImporterType));
            Assert.AreEqual(typeof(Material).FullName, matchingIssue.GetCustomProperty(BuildReportFileProperty.RuntimeType));
            Assert.That(matchingIssue.GetCustomPropertyAsInt(BuildReportFileProperty.Size), Is.Positive);
        }

#endif


        [Test]
#if !BUILD_REPORT_API_SUPPORT
        [Ignore("Not Supported in this version of Unity")]
#endif
        public void BuildReport_Steps_AreReported()
        {
            var issues = AnalyzeBuild(IssueCategory.BuildStep);
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
