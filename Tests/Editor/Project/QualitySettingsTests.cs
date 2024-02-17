using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Unity.ProjectAuditor.Editor;
using Unity.ProjectAuditor.Editor.SettingsAnalysis;
using Unity.ProjectAuditor.Editor.Tests.Common;
using UnityEngine;

namespace Unity.ProjectAuditor.EditorTests
{
    class QualitySettingsTests : TestFixtureBase
    {
        int m_InitialQualityLevel;

        [SetUp]
        public void SetUp()
        {
            m_InitialQualityLevel = QualitySettings.GetQualityLevel();
        }

        [TearDown]
        public void TearDown()
        {
            // restore quality level
            if (m_InitialQualityLevel != QualitySettings.GetQualityLevel())
                QualitySettings.SetQualityLevel(m_InitialQualityLevel);
        }

        [Test]
        public void SettingsAnalysis_Default_QualityAsyncUploadTimeSlice_IsReported()
        {
            Assert.True(QualitySettings.names.Length > 0, "Expected at least one Quality Settings entry, not zero/none. Test is incomplete.");

            QualitySettings.SetQualityLevel(0);

            var timeSlice = QualitySettings.asyncUploadTimeSlice;
            QualitySettings.asyncUploadTimeSlice = 2;

            var issues = Analyze(IssueCategory.ProjectSetting, i => i.Id.Equals(QualitySettingsAnalyzer.PAS0020));

            QualitySettings.asyncUploadTimeSlice = timeSlice;

            Assert.True(issues.Any(i => i.Location.Path.Equals("Project/Quality")));
        }

        [Test]
        public void SettingsAnalysis_NonDefault_QualityAsyncUploadTimeSlice_IsNotReported()
        {
            Assert.True(QualitySettings.names.Length > 0, "Expected at least one Quality Settings entry, not zero/none. Test is incomplete.");

            var timeSliceValues = new int[QualitySettings.names.Length];

            for (int i = 0; i < QualitySettings.names.Length; ++i)
            {
                QualitySettings.SetQualityLevel(i);
                timeSliceValues[i] = QualitySettings.asyncUploadTimeSlice;
                QualitySettings.asyncUploadTimeSlice = 10;
            }

            var issues = Analyze(IssueCategory.ProjectSetting, i => i.Id.Equals(QualitySettingsAnalyzer.PAS0020));

            for (int i = 0; i < QualitySettings.names.Length; ++i)
            {
                QualitySettings.SetQualityLevel(i);
                QualitySettings.asyncUploadTimeSlice = timeSliceValues[i];
            }

            Assert.True(issues.Length == 0);
        }

        [Test]
        public void SettingsAnalysis_Default_QualityAsyncUploadBufferSize_IsReported()
        {
            Assert.True(QualitySettings.names.Length > 0, "Expected at least one Quality Settings entry, not zero/none. Test is incomplete.");

            QualitySettings.SetQualityLevel(0);

            var bufferSize = QualitySettings.asyncUploadBufferSize;
            QualitySettings.asyncUploadBufferSize = 4;

            var issues = Analyze(IssueCategory.ProjectSetting, i => i.Id.Equals(QualitySettingsAnalyzer.PAS0021));

            QualitySettings.asyncUploadBufferSize = bufferSize;

            Assert.True(issues.Any(i => i.Location.Path.Equals("Project/Quality")));
        }

        [Test]
        public void SettingsAnalysis_NonDefault_QualityAsyncUploadBufferSize_IsNotReported()
        {
            Assert.True(QualitySettings.names.Length > 0, "Expected at least one Quality Settings entry, not zero/none. Test is incomplete.");

            var bufferValues = new int[QualitySettings.names.Length];

            for (int i = 0; i < QualitySettings.names.Length; ++i)
            {
                QualitySettings.SetQualityLevel(i);
                bufferValues[i] = QualitySettings.asyncUploadBufferSize;
                QualitySettings.asyncUploadBufferSize = 10;
            }

            var issues = Analyze(IssueCategory.ProjectSetting, i => i.Id.Equals(QualitySettingsAnalyzer.PAS0021));

            for (int i = 0; i < QualitySettings.names.Length; ++i)
            {
                QualitySettings.SetQualityLevel(i);
                QualitySettings.asyncUploadBufferSize = bufferValues[i];
            }

            Assert.True(issues.Length == 0);
        }

        [Test]
        public void SettingsAnalysis_Quality_Disabled_TextureStreaming_IsReported()
        {
            Assert.True(QualitySettings.names.Length > 0, "Expected at least one Quality Settings entry, not zero/none. Test is incomplete.");

            var settingsName = QualitySettings.names[0];

            QualitySettings.SetQualityLevel(0);

            var mipmapsActive = QualitySettings.streamingMipmapsActive;
            QualitySettings.streamingMipmapsActive = false;

            var issues = Analyze(IssueCategory.ProjectSetting, i => i.Id.Equals(QualitySettingsAnalyzer.PAS1007));

            QualitySettings.streamingMipmapsActive = mipmapsActive;

            Assert.True(issues.Any(i => i.Location.Path.Equals("Project/Quality")));
        }

        [Test]
        [Ignore("TODO: investigate reason for test failure")]
        public void SettingsAnalysis_Quality_Enabled_TextureStreaming_IsNotReported()
        {
            Assert.True(QualitySettings.names.Length > 0, "Expected at least one Quality Settings entry, not zero/none. Test is incomplete.");

            var settingsName = QualitySettings.names[0];

            QualitySettings.SetQualityLevel(0);

            var mipmapsActive = QualitySettings.streamingMipmapsActive;
            QualitySettings.streamingMipmapsActive = true;

            var issues = Analyze(IssueCategory.ProjectSetting, i => i.Id.Equals(QualitySettingsAnalyzer.PAS1007));

            QualitySettings.streamingMipmapsActive = mipmapsActive;

            Assert.True(issues.Any(i => i.Location.Path.Equals("Project/Quality/" + settingsName)) == false);
        }

        [Test]
        public void SettingsAnalysis_MipmapStreaming_Disabled_Reported()
        {
            var qualityLevelsValues = new List<bool>();

            for (var i = 0; i < QualitySettings.names.Length; i++)
            {
                QualitySettings.SetQualityLevel(i);
                qualityLevelsValues.Add(QualitySettings.streamingMipmapsActive);
                QualitySettings.streamingMipmapsActive = false;

                var id = QualitySettingsAnalyzer.PAS1007;
                var issues = Analyze(IssueCategory.ProjectSetting, j => j.Id.Equals(id));
                var qualitySettingIssue = issues.FirstOrDefault();

                Assert.NotNull(qualitySettingIssue);
            }

            ResetQualityLevelsValues(qualityLevelsValues);
        }

        [Test]
        public void SettingsAnalysis_MipmapStreaming_Enabled_IsNotReported()
        {
            var qualityLevelsValues = new List<bool>();

            for (var i = 0; i < QualitySettings.names.Length; i++)
            {
                QualitySettings.SetQualityLevel(i);
                qualityLevelsValues.Add(QualitySettings.streamingMipmapsActive);

                QualitySettings.streamingMipmapsActive = true;
            }

            var id = QualitySettingsAnalyzer.PAS1007;
            var issues = Analyze(IssueCategory.ProjectSetting, j => j.Id.Equals(id));
            var qualitySettingIssue = issues.FirstOrDefault();

            Assert.IsNull(qualitySettingIssue);

            ResetQualityLevelsValues(qualityLevelsValues);
        }

        [Test]
        public void SettingsAnalysis_Enable_StreamingMipmap()
        {
            var qualityLevelsValues = new List<bool>();

            for (var i = 0; i < QualitySettings.names.Length; i++)
            {
                QualitySettings.SetQualityLevel(i);
                qualityLevelsValues.Add(QualitySettings.streamingMipmapsActive);
                QualitySettings.streamingMipmapsActive = false;

                QualitySettingsAnalyzer.EnableStreamingMipmap(i);
                Assert.IsTrue(QualitySettings.streamingMipmapsActive);
            }

            ResetQualityLevelsValues(qualityLevelsValues);
        }

        void ResetQualityLevelsValues(List<bool> values)
        {
            for (var i = 0; i < QualitySettings.names.Length; i++)
            {
                QualitySettings.SetQualityLevel(i);
                QualitySettings.streamingMipmapsActive = values[i];
            }
        }
    }
}
