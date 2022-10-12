using NUnit.Framework;
using Unity.ProjectAuditor.Editor;
using Unity.ProjectAuditor.Editor.Modules;
using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;

namespace Unity.ProjectAuditor.EditorTests
{
    class TextureTests : TestFixtureBase
    {
        const string k_TextureName = "ProceduralTextureForTest3212";
        const int k_Resolution = 1;

        TempAsset m_TempTexture;

        [OneTimeSetUp]
        public void SetUp()
        {
            var texture = new Texture2D(k_Resolution, k_Resolution);
            texture.SetPixel(0, 0, Random.ColorHSV());
            texture.name = k_TextureName;
            texture.Apply();

            m_TempTexture = new TempAsset(k_TextureName + ".png", texture.EncodeToPNG());
        }

        [Test]
        public void Texture_Properties_AreReported()
        {
            var textureTests = AnalyzeAndFindAssetIssues(m_TempTexture, IssueCategory.Texture);

            Assert.AreEqual(1, textureTests.Length);

            var reportedTexture = textureTests[0];
            var textureImporter = (AssetImporter.GetAtPath(m_TempTexture.relativePath) as TextureImporter);

            Assert.NotNull(textureImporter);
            Assert.AreEqual(k_TextureName, reportedTexture.description, "Checked Texture Name");

            Assert.AreEqual(textureImporter.textureShape.ToString(), reportedTexture.GetCustomProperty(TextureProperty.Shape), "Checked Texture Shape/Dimension");

            Assert.AreEqual(textureImporter.textureType.ToString(), reportedTexture.GetCustomProperty(TextureProperty.ImporterType), "Checked TextureImporterType ");

            Assert.AreEqual("AutomaticCompressed", reportedTexture.GetCustomProperty(TextureProperty.Format), "Checked Texture Format");

            Assert.AreEqual(textureImporter.textureCompression.ToString(), reportedTexture.GetCustomProperty(TextureProperty.TextureCompression), "Checked Texture Compression");

            Assert.AreEqual("True", reportedTexture.GetCustomProperty(TextureProperty.MipMapEnabled), "Checked MipMaps Enabled");

            Assert.AreEqual("False", reportedTexture.GetCustomProperty(TextureProperty.Readable), "Checked Texture Read/Write");

            Assert.AreEqual((k_Resolution + "x" + k_Resolution), reportedTexture.GetCustomProperty(TextureProperty.Resolution), "Checked Texture Resolution");

            var texture = AssetDatabase.LoadAssetAtPath<Texture>(m_TempTexture.relativePath);

            /*
            Assert.NotNull(texture);
            Assert.AreEqual(Profiler.GetRuntimeMemorySizeLong(texture).ToString(), reportedTexture.GetCustomProperty(TextureProperty.SizeOnDisk), "Checked Texture Size");
            */

            Assert.AreEqual(EditorUserBuildSettings.activeBuildTarget.ToString(), reportedTexture.GetCustomProperty(TextureProperty.Platform), "Checked Platform");
        }
    }
}
