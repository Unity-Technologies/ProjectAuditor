using System.Linq;
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
        const string k_TextureNameMipMapDefault = k_TextureName + "MipMapDefaultTest1234";
        const string k_TextureNameNoMipMapDefault = k_TextureName + "NoMipMapDefaultTest1234";
        const string k_TextureNameMipMapGUI = k_TextureName + "MipMapGUITest1234";
        const string k_TextureNameMipMapSprite = k_TextureName + "MipMapSpriteTest1234";

        const int k_Resolution = 1;

        TempAsset m_TempTexture;
        TempAsset m_TempTextureMipMapDefault;
        TempAsset m_TempTextureNoMipMapDefault;
        TempAsset m_TempTextureMipMapGUI;
        TempAsset m_TempTextureMipMapSprite;

        [OneTimeSetUp]
        public void SetUp()
        {
            var texture = new Texture2D(k_Resolution, k_Resolution);
            texture.SetPixel(0, 0, Random.ColorHSV());
            texture.name = k_TextureName;
            texture.Apply();

            var encodedPNG = texture.EncodeToPNG();

            m_TempTexture = new TempAsset(k_TextureName + ".png", encodedPNG);

            m_TempTextureMipMapDefault = new TempAsset(k_TextureNameMipMapDefault + ".png", encodedPNG);

            var textureImporter = AssetImporter.GetAtPath(m_TempTextureMipMapDefault.relativePath) as TextureImporter;
            textureImporter.textureType = TextureImporterType.Default;
            textureImporter.mipmapEnabled = true;
            textureImporter.SaveAndReimport();

            m_TempTextureNoMipMapDefault = new TempAsset(k_TextureNameNoMipMapDefault + ".png", encodedPNG);

            textureImporter = AssetImporter.GetAtPath(m_TempTextureNoMipMapDefault.relativePath) as TextureImporter;
            textureImporter.textureType = TextureImporterType.Default;
            textureImporter.mipmapEnabled = false;
            textureImporter.SaveAndReimport();

            m_TempTextureMipMapGUI = new TempAsset(k_TextureNameMipMapGUI + ".png", encodedPNG);

            textureImporter = AssetImporter.GetAtPath(m_TempTextureMipMapGUI.relativePath) as TextureImporter;
            textureImporter.textureType = TextureImporterType.GUI;
            textureImporter.mipmapEnabled = true;
            textureImporter.SaveAndReimport();

            m_TempTextureMipMapSprite = new TempAsset(k_TextureNameMipMapSprite + ".png", encodedPNG);

            textureImporter = AssetImporter.GetAtPath(m_TempTextureMipMapSprite.relativePath) as TextureImporter;
            textureImporter.textureType = TextureImporterType.Sprite;
            textureImporter.mipmapEnabled = true;
            textureImporter.SaveAndReimport();
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

        [Test]
        public void Texture_MipMapUnused_IsReported()
        {
            var textureDiagnostic = AnalyzeAndFindAssetIssues(m_TempTextureNoMipMapDefault, IssueCategory.AssetDiagnostic).FirstOrDefault(i => i.descriptor.Equals(TextureAnalyzer.k_TextureMipMapNotEnabledDescriptor));

            Assert.NotNull(textureDiagnostic);
        }

        [Test]
        public void Texture_MipMapUnused_IsNotReported()
        {
            var textureDiagnostic = AnalyzeAndFindAssetIssues(m_TempTextureMipMapDefault, IssueCategory.AssetDiagnostic).FirstOrDefault(i => i.descriptor.Equals(TextureAnalyzer.k_TextureMipMapNotEnabledDescriptor));

            Assert.Null(textureDiagnostic);
        }

        [Test]
        public void Texture_MipMapUsedForGUI_IsReported()
        {
            var textureDiagnostic = AnalyzeAndFindAssetIssues(m_TempTextureMipMapGUI, IssueCategory.AssetDiagnostic).FirstOrDefault(i => i.descriptor.Equals(TextureAnalyzer.k_TextureMipMapEnabledDescriptor));

            Assert.NotNull(textureDiagnostic);
        }

        [Test]
        public void Texture_MipMapUsedForSprite_IsReported()
        {
            var textureDiagnostic = AnalyzeAndFindAssetIssues(m_TempTextureMipMapSprite, IssueCategory.AssetDiagnostic).FirstOrDefault(i => i.descriptor.Equals(TextureAnalyzer.k_TextureMipMapEnabledDescriptor));

            Assert.NotNull(textureDiagnostic);
        }
    }
}
