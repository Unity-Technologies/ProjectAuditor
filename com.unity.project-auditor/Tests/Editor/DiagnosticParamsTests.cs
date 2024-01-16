using System;
using System.Linq;
using Newtonsoft.Json;
using NUnit.Framework;
using Unity.ProjectAuditor.Editor;
using Unity.ProjectAuditor.Editor.Modules;
using Unity.ProjectAuditor.Editor.Tests.Common;
using UnityEditor;
using UnityEngine;

namespace Unity.ProjectAuditor.EditorTests
{
    [Serializable]
    class DiagnosticParamsTests : TestFixtureBase
    {
        const string k_TextureName = "RuleTestTexture";
        const int k_TestTextureResolution = 64;
        TestAsset m_TestTextureAsset;

        DiagnosticParams m_DiagnosticParams;

        const string k_TextureStreamingMipmapsSizeLimit = "TextureStreamingMipmapsSizeLimit";
        const int k_MipmapSizeLimitDefault = 4000;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            m_DiagnosticParams = new DiagnosticParams();

            var texture = new Texture2D(k_TestTextureResolution, k_TestTextureResolution);
            texture.SetPixel(0, 0, Color.red);
            texture.name = k_TextureName;
            texture.Apply();
            m_TestTextureAsset = new TestAsset(k_TextureName + ".png", texture.EncodeToPNG());

            var importer =
                AssetImporter.GetAtPath(m_TestTextureAsset.RelativePath) as TextureImporter;
            importer.mipmapEnabled = true;
            importer.streamingMipmaps = false;
            //Size should not be compressed for testing purposes.
            //If compressed, it won't trigger a warning, as size will be below the minimal size
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }

        [Test]
        public void DiagnosticParams_CanCreateAndEditCustomDiagnosticParams()
        {
            ValidateTargetPlatform();

            m_DiagnosticParams.ClearAllParameters();
            Assert.Zero(m_DiagnosticParams.CountParameters());
            m_DiagnosticParams.RegisterParameter(k_TextureStreamingMipmapsSizeLimit, k_MipmapSizeLimitDefault);
            m_DiagnosticParams.SetAnalysisPlatform(m_Platform);

            var paramVal = m_DiagnosticParams.GetParameter(k_TextureStreamingMipmapsSizeLimit);
            Assert.AreEqual(paramVal, k_MipmapSizeLimitDefault);

            m_DiagnosticParams.SetParameter(k_TextureStreamingMipmapsSizeLimit, 32);
            paramVal = m_DiagnosticParams.GetParameter(k_TextureStreamingMipmapsSizeLimit);
            Assert.AreEqual(paramVal, 32);

            m_DiagnosticParams.SetParameter(k_TextureStreamingMipmapsSizeLimit, 64, m_Platform);
            paramVal = m_DiagnosticParams.GetParameter(k_TextureStreamingMipmapsSizeLimit);
            Assert.AreEqual(paramVal, 64);
        }

        [Test]
        public void DiagnosticParams_CustomDiagnosticParamsImpactReports()
        {
            ValidateTargetPlatform();

            m_DiagnosticParams.ClearAllParameters();
            m_DiagnosticParams.RegisterParameters();
            m_DiagnosticParams.SetAnalysisPlatform(BuildTarget.NoTarget);

            var analysisParams = new AnalysisParams
            {
                Categories = new[] { IssueCategory.AssetIssue },
                DiagnosticParams = m_DiagnosticParams
            };

            var projectAuditor = new Editor.ProjectAuditor();
            var report = projectAuditor.Audit(analysisParams);
            var foundIssues = report.GetAllIssues().Where(i => i.RelativePath.Equals(m_TestTextureAsset.RelativePath));

            Assert.NotNull(foundIssues);
            Assert.Null(foundIssues.FirstOrDefault(i => i.Id.Equals(TextureAnalyzer.k_TextureStreamingMipMapEnabledDescriptor.Id)));

            // Texture would normally be too small to trigger this diagnostic, unless we specify a custom smaller limit
            analysisParams.DiagnosticParams.SetParameter(k_TextureStreamingMipmapsSizeLimit, 32);
            report = projectAuditor.Audit(analysisParams);
            foundIssues = report.GetAllIssues().Where(i => i.RelativePath.Equals(m_TestTextureAsset.RelativePath));

            Assert.NotNull(foundIssues);
            Assert.NotNull(foundIssues.FirstOrDefault(i => i.Id.Equals(TextureAnalyzer.k_TextureStreamingMipMapEnabledDescriptor.Id)));
        }

        [Test]
        public void DiagnosticParams_CanSerializeAndDeserialize()
        {
            m_DiagnosticParams.ClearAllParameters();
            Assert.Zero(m_DiagnosticParams.CountParameters());

            m_DiagnosticParams.SetAnalysisPlatform(BuildTarget.Android);
            Assert.AreEqual(1, m_DiagnosticParams.CurrentParamsIndex);

            m_DiagnosticParams.RegisterParameter(k_TextureStreamingMipmapsSizeLimit, k_MipmapSizeLimitDefault);
            Assert.NotZero(m_DiagnosticParams.CountParameters());

            var paramVal = m_DiagnosticParams.GetParameter(k_TextureStreamingMipmapsSizeLimit);
            Assert.AreEqual(paramVal, k_MipmapSizeLimitDefault);

            var jsonString = JsonConvert.SerializeObject(m_DiagnosticParams, Formatting.None,
                new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                });

            Assert.NotNull(jsonString);

            m_DiagnosticParams.ClearAllParameters();
            Assert.Zero(m_DiagnosticParams.CountParameters());

            m_DiagnosticParams.SetAnalysisPlatform(BuildTarget.NoTarget);
            Assert.AreEqual(0, m_DiagnosticParams.CurrentParamsIndex);

            m_DiagnosticParams = JsonConvert.DeserializeObject<DiagnosticParams>(jsonString, new JsonSerializerSettings
            {
                ObjectCreationHandling = ObjectCreationHandling.Replace
            });

            Assert.AreEqual(1, m_DiagnosticParams.CurrentParamsIndex);
            Assert.NotZero(m_DiagnosticParams.CountParameters());

            paramVal = m_DiagnosticParams.GetParameter(k_TextureStreamingMipmapsSizeLimit);
            Assert.AreEqual(paramVal, k_MipmapSizeLimitDefault);
        }
    }
}
