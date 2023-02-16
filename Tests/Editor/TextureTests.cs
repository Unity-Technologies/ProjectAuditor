using System.Linq;
using NUnit.Framework;
using Unity.ProjectAuditor.Editor;
using Unity.ProjectAuditor.Editor.Modules;
using Unity.ProjectAuditor.Editor.TestUtils;
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
        const string k_TextureNameReadWriteEnabled = k_TextureName + "ReadWriteEnabledTest1234";

        const int k_Resolution = 1;

        TestAsset m_TestTexture;
        TestAsset m_TestTextureMipMapDefault;
        TestAsset m_TestTextureNoMipMapDefault;
        TestAsset m_TestTextureMipMapGui;
        TestAsset m_TestTextureMipMapSprite;
        TestAsset m_TestTextureReadWriteEnabled;

        [OneTimeSetUp]
        public void SetUp()
        {
            var texture = new Texture2D(k_Resolution, k_Resolution);
            texture.SetPixel(0, 0, Random.ColorHSV());
            texture.name = k_TextureName;
            texture.Apply();

            var encodedPNG = texture.EncodeToPNG();

            m_TestTexture = new TestAsset(k_TextureName + ".png", encodedPNG);

            m_TestTextureMipMapDefault = new TestAsset(k_TextureNameMipMapDefault + ".png", encodedPNG);

            var textureImporter = AssetImporter.GetAtPath(m_TestTextureMipMapDefault.relativePath) as TextureImporter;
            textureImporter.textureType = TextureImporterType.Default;
            textureImporter.mipmapEnabled = true;
            textureImporter.SaveAndReimport();

            m_TestTextureNoMipMapDefault = new TestAsset(k_TextureNameNoMipMapDefault + ".png", encodedPNG);

            textureImporter = AssetImporter.GetAtPath(m_TestTextureNoMipMapDefault.relativePath) as TextureImporter;
            textureImporter.textureType = TextureImporterType.Default;
            textureImporter.mipmapEnabled = false;
            textureImporter.SaveAndReimport();

            m_TestTextureMipMapGui = new TestAsset(k_TextureNameMipMapGUI + ".png", encodedPNG);

            textureImporter = AssetImporter.GetAtPath(m_TestTextureMipMapGui.relativePath) as TextureImporter;
            textureImporter.textureType = TextureImporterType.GUI;
            textureImporter.mipmapEnabled = true;
            textureImporter.SaveAndReimport();

            m_TestTextureMipMapSprite = new TestAsset(k_TextureNameMipMapSprite + ".png", encodedPNG);

            textureImporter = AssetImporter.GetAtPath(m_TestTextureMipMapSprite.relativePath) as TextureImporter;
            textureImporter.textureType = TextureImporterType.Sprite;
            textureImporter.mipmapEnabled = true;
            textureImporter.SaveAndReimport();

            m_TestTextureReadWriteEnabled = new TestAsset(k_TextureNameReadWriteEnabled + ".png", encodedPNG);

            textureImporter = AssetImporter.GetAtPath(m_TestTextureReadWriteEnabled.relativePath) as TextureImporter;
            textureImporter.isReadable = true;
            textureImporter.SaveAndReimport();
        }

        [Test]
        public void Texture_Properties_AreReported()
        {
            var textureImporter = (AssetImporter.GetAtPath(m_TestTexture.relativePath) as TextureImporter);

            Assert.NotNull(textureImporter);

            var reportedTextures = AnalyzeAndFindAssetIssues(m_TestTexture, IssueCategory.Texture);

            Assert.AreEqual(1, reportedTextures.Length);

            var texture = reportedTextures[0];
            Assert.AreEqual(k_TextureName, texture.description);

            Assert.AreEqual(textureImporter.textureShape.ToString(), texture.GetCustomProperty(TextureProperty.Shape));
            Assert.AreEqual(textureImporter.textureType.ToString(), texture.GetCustomProperty(TextureProperty.ImporterType));
            Assert.AreEqual(textureImporter.textureCompression.ToString(), texture.GetCustomProperty(TextureProperty.TextureCompression));

            Assert.AreEqual("AutomaticCompressed", texture.GetCustomProperty(TextureProperty.Format));
            Assert.True(texture.GetCustomPropertyBool(TextureProperty.MipMapEnabled));
            Assert.False(texture.GetCustomPropertyBool(TextureProperty.Readable));
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
            var textureDiagnostic = AnalyzeAndFindAssetIssues(m_TestTextureNoMipMapDefault, IssueCategory.AssetDiagnostic).FirstOrDefault(i => i.descriptor.Equals(TextureAnalyzer.k_TextureMipMapNotEnabledDescriptor));

            Assert.NotNull(textureDiagnostic);
        }

        [Test]
        public void Texture_MipMapUnused_IsNotReported()
        {
            var textureDiagnostic = AnalyzeAndFindAssetIssues(m_TestTextureMipMapDefault, IssueCategory.AssetDiagnostic).FirstOrDefault(i => i.descriptor.Equals(TextureAnalyzer.k_TextureMipMapNotEnabledDescriptor));

            Assert.Null(textureDiagnostic);
        }

        [Test]
        public void Texture_MipMapUsedForGUI_IsReported()
        {
            var textureDiagnostic = AnalyzeAndFindAssetIssues(m_TestTextureMipMapGui, IssueCategory.AssetDiagnostic).FirstOrDefault(i => i.descriptor.Equals(TextureAnalyzer.k_TextureMipMapEnabledDescriptor));

            Assert.NotNull(textureDiagnostic);
            Assert.NotNull(textureDiagnostic.descriptor);
            Assert.NotNull(textureDiagnostic.descriptor.fixer);

            textureDiagnostic.descriptor.Fix(textureDiagnostic);

            textureDiagnostic = AnalyzeAndFindAssetIssues(m_TestTextureMipMapGui, IssueCategory.AssetDiagnostic).FirstOrDefault(i => i.descriptor.Equals(TextureAnalyzer.k_TextureMipMapEnabledDescriptor));

            Assert.Null(textureDiagnostic);
        }

        [Test]
        public void Texture_MipMapUsedForSprite_IsReportedAndFixed()
        {
            var textureDiagnostic = AnalyzeAndFindAssetIssues(m_TestTextureMipMapSprite, IssueCategory.AssetDiagnostic).FirstOrDefault(i => i.descriptor.Equals(TextureAnalyzer.k_TextureMipMapEnabledDescriptor));

            Assert.NotNull(textureDiagnostic);
            Assert.NotNull(textureDiagnostic.descriptor);
            Assert.NotNull(textureDiagnostic.descriptor.fixer);

            textureDiagnostic.descriptor.Fix(textureDiagnostic);

            textureDiagnostic = AnalyzeAndFindAssetIssues(m_TestTextureMipMapSprite, IssueCategory.AssetDiagnostic).FirstOrDefault(i => i.descriptor.Equals(TextureAnalyzer.k_TextureMipMapEnabledDescriptor));

            Assert.Null(textureDiagnostic);
        }

        [Test]
        public void Texture_ReadWriteEnabled_IsReported()
        {
            var textureDiagnostic = AnalyzeAndFindAssetIssues(m_TestTextureReadWriteEnabled, IssueCategory.AssetDiagnostic).FirstOrDefault(i => i.descriptor.Equals(TextureAnalyzer.k_TextureReadWriteEnabledDescriptor));

            Assert.NotNull(textureDiagnostic);
            Assert.NotNull(textureDiagnostic.descriptor);
            Assert.NotNull(textureDiagnostic.descriptor.fixer);

            textureDiagnostic.descriptor.Fix(textureDiagnostic);

            textureDiagnostic = AnalyzeAndFindAssetIssues(m_TestTextureReadWriteEnabled, IssueCategory.AssetDiagnostic).FirstOrDefault(i => i.descriptor.Equals(TextureAnalyzer.k_TextureReadWriteEnabledDescriptor));

            Assert.Null(textureDiagnostic);
        }

        [Test]
        public void Texture_ReadWriteEnabled_IsNotReported()
        {
            var textureDiagnostic = AnalyzeAndFindAssetIssues(m_TestTextureNoMipMapDefault, IssueCategory.AssetDiagnostic).FirstOrDefault(i => i.descriptor.Equals(TextureAnalyzer.k_TextureReadWriteEnabledDescriptor));

            Assert.IsNull(textureDiagnostic);
        }
    }
}
