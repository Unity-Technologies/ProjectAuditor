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
            var textureImporter = (AssetImporter.GetAtPath(m_TempTexture.relativePath) as TextureImporter);

            Assert.NotNull(textureImporter);

            var reportedTextures = AnalyzeAndFindAssetIssues(m_TempTexture, IssueCategory.Texture);

            Assert.AreEqual(1, reportedTextures.Length);

            var texture = reportedTextures[0];
            Assert.AreEqual(k_TextureName, texture.description);

            Assert.AreEqual(textureImporter.textureShape.ToString(), texture.GetCustomProperty(TextureProperty.Shape));
            Assert.AreEqual(textureImporter.textureType.ToString(), texture.GetCustomProperty(TextureProperty.ImporterType));
            Assert.AreEqual(textureImporter.textureCompression.ToString(), texture.GetCustomProperty(TextureProperty.TextureCompression));

            Assert.AreEqual("AutomaticCompressed", texture.GetCustomProperty(TextureProperty.Format));
            Assert.True(texture.GetCustomPropertyAsBool(TextureProperty.MipMapEnabled));
            Assert.False(texture.GetCustomPropertyAsBool(TextureProperty.Readable));
            Assert.AreEqual((k_Resolution + "x" + k_Resolution), texture.GetCustomProperty(TextureProperty.Resolution));

            /*
            var texture = AssetDatabase.LoadAssetAtPath<Texture>(m_TempTexture.relativePath);
            Assert.NotNull(texture);
            Assert.AreEqual(Profiler.GetRuntimeMemorySizeLong(texture).ToString(), reportedTexture.GetCustomProperty(TextureProperty.SizeOnDisk), "Checked Texture Size");
            */
        }
    }
}
