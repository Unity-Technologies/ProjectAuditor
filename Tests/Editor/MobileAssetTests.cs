using System.IO;
using NUnit.Framework;
using Unity.ProjectAuditor.Editor;
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
        public void Android_StreamingAssetsFolderTooLarge_IsReported()
        {
            CreateTemporaryStreamingAssets();

            var textureDiagnostic = Analyze(IssueCategory.AssetDiagnostic, issue => issue.descriptor.id == "PAA0001");

            RemoveTemporaryStreamingAssets();

            Assert.IsNotEmpty(textureDiagnostic);
        }

        [Test]
        [RequirePlatformSupport(BuildTarget.iOS)]
        public void iOS_StreamingAssetsFolderTooLarge_IsReported()
        {
            CreateTemporaryStreamingAssets();

            var textureDiagnostic = Analyze(IssueCategory.AssetDiagnostic, issue => issue.descriptor.id == "PAA0001");

            RemoveTemporaryStreamingAssets();

            Assert.IsNotEmpty(textureDiagnostic);
        }
    }
}
