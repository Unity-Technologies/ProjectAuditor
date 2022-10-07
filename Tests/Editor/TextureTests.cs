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
        const int k_resolution = 1;

        Texture m_texture;
        TextureImporter m_textureViaImporter;
        TempAsset m_tempTestTexture;

        [OneTimeSetUp]
        public void SetUp()
        {
            var textureTemp = new Texture2D(k_resolution, k_resolution); 
            textureTemp.SetPixel(0, 0, Random.ColorHSV());
            textureTemp.name = "ProceduralTextureForTest3212";
            textureTemp.Apply();
            m_tempTestTexture = new TempAsset(textureTemp.name + ".png", textureTemp.EncodeToPNG());

            m_texture = (Texture)AssetDatabase.LoadAssetAtPath(m_tempTestTexture.relativePath, typeof(Texture));
            m_textureViaImporter = (AssetImporter.GetAtPath(m_tempTestTexture.relativePath) as TextureImporter); 
        }

        [Test]
        [Explicit]
        public void Texture_Properties_AreReported()
        {
            var textureTests = Analyze(IssueCategory.Texture);
            var testedTexture = textureTests[0];

            foreach (var testedIssue in textureTests)
            {
                if (testedIssue.filename == m_texture.name) { testedTexture = testedIssue; }
            }

            Assert.AreEqual(m_texture.name, testedTexture.GetCustomProperty(TextureProperties.Name), "Checked Texture Name");

            Assert.AreEqual(m_textureViaImporter.textureShape.ToString(), testedTexture.GetCustomProperty(TextureProperties.Shape), "Checked Texture Shape/Dimension");

            Assert.AreEqual(m_textureViaImporter.textureType.ToString(), testedTexture.GetCustomProperty(TextureProperties.ImporterType), "Checked TextureImporterType ");

            Assert.AreEqual("AutomaticCompressed", testedTexture.GetCustomProperty(TextureProperties.Format), "Checked Texture Format");

            Assert.AreEqual(m_textureViaImporter.textureCompression.ToString(), testedTexture.GetCustomProperty(TextureProperties.TextureCompression), "Checked Texture Compression");

            Assert.AreEqual("True", testedTexture.GetCustomProperty(TextureProperties.MipMapEnabled), "Checked MipMaps Enabled");

            Assert.AreEqual("False", testedTexture.GetCustomProperty(TextureProperties.Readable), "Checked Texture Read/Write");

            Assert.AreEqual((k_resolution + "x" + k_resolution), testedTexture.GetCustomProperty(TextureProperties.Resolution), "Checked Texture Resolution");

            Assert.AreEqual(Profiler.GetRuntimeMemorySizeLong(m_texture).ToString(), testedTexture.GetCustomProperty(TextureProperties.SizeOnDisk), "Checked Texture Size");

            Assert.AreEqual(EditorUserBuildSettings.activeBuildTarget.ToString(), testedTexture.GetCustomProperty(TextureProperties.Platform), "Checked Platform");
        }
    }
}
