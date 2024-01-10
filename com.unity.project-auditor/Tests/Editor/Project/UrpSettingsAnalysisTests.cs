using System.Linq;
using NUnit.Framework;
using Unity.ProjectAuditor.Editor;
using Unity.ProjectAuditor.Editor.SettingsAnalysis;
using Unity.ProjectAuditor.Editor.Tests.Common;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;
using UnityEditor.TestTools;

#if PACKAGE_URP
using UnityEngine.Rendering.Universal;
#endif

namespace Unity.ProjectAuditor.EditorTests
{
    class UrpSettingsAnalysisTests : TestFixtureBase
    {
        [OneTimeSetUp]
        public void SetUp()
        {
            m_Platform = BuildTarget.Android;
        }

        [Test]
        public void SrpAssetSettingsAnalysis_SrpBatching_IsNotReportedOnceFixed()
        {
            var defaultRP = GraphicsSettings.defaultRenderPipeline;
            var qualityRP = QualitySettings.renderPipeline;

            if (defaultRP != null)
            {
                TestSrpBatchingSetting(defaultRP, -1);
            }

            if (qualityRP != null)
            {
                TestSrpBatchingSetting(qualityRP, QualitySettings.GetQualityLevel());
            }
        }

        void TestSrpBatchingSetting(RenderPipelineAsset renderPipeline, int qualityLevel)
        {
            bool? initialSetting = SrpAssetSettingsAnalyzer.GetSrpBatcherSetting(renderPipeline);

            SrpAssetSettingsAnalyzer.SetSrpBatcherSetting(renderPipeline, false);
            var issues = Analyze(IssueCategory.ProjectSetting,
                i => i.Id.IsValid() && i.Id.GetDescriptor().Id == SrpAssetSettingsAnalyzer.PAS1008);
            var srpBatchingIssue = issues.FirstOrDefault();
            Assert.NotNull(srpBatchingIssue);
            Assert.IsTrue(issues.Any(i => i.GetCustomPropertyInt32(0) == qualityLevel),
                $"Render Pipeline with quality level {qualityLevel} should have disabled SRP Batcher.");

            SrpAssetSettingsAnalyzer.SetSrpBatcherSetting(renderPipeline, true);
            issues = Analyze(IssueCategory.ProjectSetting,
                i => i.Id.IsValid() && i.Id.GetDescriptor().Id == SrpAssetSettingsAnalyzer.PAS1008);
            Assert.IsFalse(issues.Any(i => i.GetCustomPropertyInt32(0) == qualityLevel),
                $"Render Pipeline with quality level {qualityLevel} should have enabled SRP Batcher.");

            if (initialSetting != null)
            {
                SrpAssetSettingsAnalyzer.SetSrpBatcherSetting(renderPipeline, initialSetting.Value);
            }
        }

#if PACKAGE_URP
        [Test]
        public void UrpAssetIsSpecifiedAnalysis_IsNotReportedOnceFixed()
        {
            var defaultRP = GraphicsSettings.defaultRenderPipeline;
            var qualityRP = QualitySettings.renderPipeline;

            if (defaultRP != null || qualityRP != null)
            {
                GraphicsSettings.defaultRenderPipeline = null;
                QualitySettings.renderPipeline = null;

                const string urpAssetTitle = "URP: URP Asset is not specified";
                var issues = Analyze(IssueCategory.ProjectSetting,
                    i => i.Id.GetDescriptor().Title.Equals(urpAssetTitle));
                var urpIssue = issues.FirstOrDefault();
                Assert.NotNull(urpIssue);

                GraphicsSettings.defaultRenderPipeline = defaultRP;
                QualitySettings.renderPipeline = qualityRP;

                issues = Analyze(IssueCategory.ProjectSetting,
                    i => i.Id.GetDescriptor().Title.Equals(urpAssetTitle));
                urpIssue = issues.FirstOrDefault();
                Assert.Null(urpIssue);
            }
        }

        [Test]
        [RequirePlatformSupport(BuildTarget.Android)]
        public void UrpCameraStopNaNAnalysis_IsNotReportedOnceFixed()
        {
            var cameraData = RenderPipelineUtils
                .GetAllComponents<UniversalAdditionalCameraData>().FirstOrDefault();
            if (cameraData != null)
            {
                const string stopNaNTitle = "URP: Stop NaN property is enabled";
                var initStopNaN = cameraData.stopNaN;

                cameraData.stopNaN = true;
                var issues = Analyze(IssueCategory.ProjectSetting,
                    i => i.Id.GetDescriptor().Title.Equals(stopNaNTitle));
                var issuesLength = issues.Length;
                Assert.IsTrue(issuesLength > 0);

                cameraData.stopNaN = false;
                issues = Analyze(IssueCategory.ProjectSetting,
                    i => i.Id.GetDescriptor().Title.Equals(stopNaNTitle));
                var issuesLength2 = issues.Length;
                Assert.IsTrue(issuesLength - issuesLength2 == 1);

                cameraData.stopNaN = initStopNaN;
            }
        }

        [Test]
        [RequirePlatformSupport(BuildTarget.Android)]
        public void UrpAssetHdrSettingsAnalysis_IsNotReportedOnceFixed()
        {
            var defaultRP = GraphicsSettings.defaultRenderPipeline;
            var qualityRP = QualitySettings.renderPipeline;
            if (defaultRP != null)
            {
                TestUrpHdrSetting(defaultRP, -1);
            }

            if (qualityRP != null)
            {
                int qualityLevel = QualitySettings.GetQualityLevel();
                TestUrpHdrSetting(qualityRP, qualityLevel);
            }
        }

        [Test]
        [RequirePlatformSupport(BuildTarget.Android)]
        public void UrpAssetMsaaSettingsAnalysis_IsNotReportedOnceFixed()
        {
            var defaultRP = GraphicsSettings.defaultRenderPipeline;
            var qualityRP = QualitySettings.renderPipeline;
            if (defaultRP != null)
            {
                TestUrpMsaaSetting(defaultRP, -1);
            }

            if (qualityRP != null)
            {
                int qualityLevel = QualitySettings.GetQualityLevel();
                TestUrpMsaaSetting(qualityRP, qualityLevel);
            }
        }

        void TestUrpHdrSetting(RenderPipelineAsset renderPipeline, int qualityLevel)
        {
            bool initialHdrSetting = UrpAnalyzer.GetHdrSetting(renderPipeline);

            const string hdrTitle = "URP: HDR is enabled";
            UrpAnalyzer.SetHdrSetting(renderPipeline, true);
            var issues = Analyze(IssueCategory.ProjectSetting,
                i => i.Id.GetDescriptor().Title.Equals(hdrTitle));
            Assert.IsTrue(issues.Any(i => i.GetCustomPropertyInt32(0) == qualityLevel),
                $"Render Pipeline with quality level {qualityLevel} should have enabled HDR.");

            UrpAnalyzer.SetHdrSetting(renderPipeline, false);
            issues = Analyze(IssueCategory.ProjectSetting,
                i => i.Id.GetDescriptor().Title.Equals(hdrTitle));
            Assert.IsFalse(issues.Any(i => i.GetCustomPropertyInt32(0) == qualityLevel),
                $"Render Pipeline with quality level {qualityLevel} should have disabled HDR.");

            UrpAnalyzer.SetHdrSetting(renderPipeline, initialHdrSetting);
        }

        void TestUrpMsaaSetting(RenderPipelineAsset renderPipeline, int qualityLevel)
        {
            int initialMsaaSetting = UrpAnalyzer.GetMsaaSampleCountSetting(renderPipeline);

            const string msaaTitle = "URP: MSAA is set to 4x or 8x";
            UrpAnalyzer.SetMsaaSampleCountSetting(renderPipeline, 4);
            var issues = Analyze(IssueCategory.ProjectSetting,
                i => i.Id.GetDescriptor().Title.Equals(msaaTitle));
            Assert.IsTrue(issues.Any(i => i.GetCustomPropertyInt32(0) == qualityLevel),
                $"Render Pipeline with quality level {qualityLevel} should have MSAA set to 4x.");

            UrpAnalyzer.SetMsaaSampleCountSetting(renderPipeline, 2);
            issues = Analyze(IssueCategory.ProjectSetting,
                i => i.Id.GetDescriptor().Title.Equals(msaaTitle));
            Assert.IsFalse(issues.Any(i => i.GetCustomPropertyInt32(0) == qualityLevel),
                $"Render Pipeline with quality level {qualityLevel} should have MSAA set to 2x.");

            UrpAnalyzer.SetMsaaSampleCountSetting(renderPipeline, initialMsaaSetting);
        }

#endif
    }
}
