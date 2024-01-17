using System.IO;
using NUnit.Framework;
using Unity.ProjectAuditor.Editor;
using Unity.ProjectAuditor.Editor.SettingsAnalysis;
using Unity.ProjectAuditor.Editor.Tests.Common;
using UnityEditor;
using UnityEditor.TestTools;
using UnityEngine;

namespace Unity.ProjectAuditor.EditorTests
{
    class MobileAssetTests : TestFixtureBase
    {
        const string k_TextureName = "VeryLargeTextureTest60Mb.tga";
        string m_RelativeTexturePath;
        bool m_DeleteStreamingAssetsFolder;

        [OneTimeSetUp]
        public void CreateTemporaryStreamingAssets()
        {
            var texture = new Texture2D(4096, 4096);
            texture.SetPixel(0, 0, Random.ColorHSV());
            texture.name = k_TextureName;
            texture.Apply();

            var encodedTGA = texture.EncodeToTGA();

            m_RelativeTexturePath = Path.Combine("Assets", Path.Combine("StreamingAssets", k_TextureName)).Replace("\\", "/");

            var newDirectoryPath = Path.GetDirectoryName(m_RelativeTexturePath);

            if (!Directory.Exists(newDirectoryPath))
            {
                Directory.CreateDirectory(newDirectoryPath);
                m_DeleteStreamingAssetsFolder = true;
            }

            File.WriteAllBytes(m_RelativeTexturePath, encodedTGA);

            Assert.True(File.Exists(m_RelativeTexturePath));
        }

        [OneTimeTearDown]
        public void RemoveTemporaryStreamingAssets()
        {
            File.Delete(m_RelativeTexturePath);

            if (m_DeleteStreamingAssetsFolder)
            {
                Directory.Delete(Path.GetDirectoryName(m_RelativeTexturePath));
            }
        }

        [Test]
        [RequirePlatformSupport(BuildTarget.Android)]
        public void StreamingAssets_FolderTooLarge_IsReported()
        {
            var platform = m_Platform;
            m_Platform = BuildTarget.Android;

            var assetDiagnostic = Analyze(IssueCategory.ProjectSetting, issue => issue.Id == StreamingAssetsFolderAnalyzer.PAA3002);

            Assert.IsNotEmpty(assetDiagnostic);

            m_Platform = platform;
        }

        [Test]
        public void StreamingAssets_FolderTooLarge_IsNotReported()
        {
            var assetDiagnostic = Analyze(IssueCategory.ProjectSetting, issue => issue.Id == StreamingAssetsFolderAnalyzer.PAA3002);

            // it should not be reported on default analysis platform (Standalone)
            Assert.IsEmpty(assetDiagnostic);
        }
    }
}
