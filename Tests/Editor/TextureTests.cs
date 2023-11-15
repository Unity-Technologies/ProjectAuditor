using System.Linq;
using NUnit.Framework;
using Unity.ProjectAuditor.Editor;
using Unity.ProjectAuditor.Editor.Diagnostic;
using Unity.ProjectAuditor.Editor.Modules;
using Unity.ProjectAuditor.Editor.Tests.Common;
using UnityEditor;
using UnityEditor.TestTools;
using UnityEngine;

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
        const string k_TextureNameStreamingMipmapDisabled = k_TextureName + "StreamingMipmapTest1234";
        const string k_TextureNameStreamingMipmapEnabled = k_TextureName + "StreamingMipmapOnTest1234";
        const string k_TextureNameAnisotropicLevelBig = k_TextureName + "AnisotropicLevelBigText1234";
        const string k_TextureNameAnisotropicLevelOne = k_TextureName + "AnisotropicLevelOneText1234";
        const string k_TextureNameSolidColor = k_TextureName + "SolidColor";
        const string k_TextureNameNotSolidColor = k_TextureName + "NotSolidColor";
        const string k_TextureNameEmptySpace = k_TextureName + "EmptySpace";

        const int k_Resolution = 1;
        const int k_LargeSize = 4050;

        TestAsset m_TestTexture;
        TestAsset m_TestTextureMipMapDefault;
        TestAsset m_TestTextureNoMipMapDefault;
        TestAsset m_TestTextureMipMapGui;
        TestAsset m_TestTextureMipMapSprite;
        TestAsset m_TestTextureReadWriteEnabled;
        TestAsset m_TextureStreamingMipmapDisabled;
        TestAsset m_TextureStreamingMipmapEnabled;
        TestAsset m_TestTextureAnisotropicLevelBig;
        TestAsset m_TestTextureAnisotropicLevelOne;
        TestAsset m_TextureSolidColor;
        TestAsset m_TextureNotSolidColor;
        TestAsset m_TestTextureEmptySpace;

        [OneTimeSetUp]
        public void SetUp()
        {
            m_AdditionalRules.Add(new Rule
            {
                Id = TextureUtilizationAnalyzer.k_TextureSolidColorDescriptor.Id,
                Severity = Severity.Moderate
            });

            m_AdditionalRules.Add(new Rule
            {
                Id = TextureUtilizationAnalyzer.k_TextureSolidColorNoFixerDescriptor.Id,
                Severity = Severity.Moderate
            });

            m_AdditionalRules.Add(new Rule
            {
                Id = TextureUtilizationAnalyzer.k_TextureAtlasEmptyDescriptor.Id,
                Severity = Severity.Moderate
            });

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

            var largeTexture = new Texture2D(k_LargeSize, k_LargeSize);
            largeTexture.SetPixel(0, 0, Random.ColorHSV());
            largeTexture.name = k_TextureNameStreamingMipmapDisabled;
            largeTexture.Apply();

            var encodedLargePNG = largeTexture.EncodeToPNG();
            m_TextureStreamingMipmapDisabled =
                new TestAsset(k_TextureNameStreamingMipmapDisabled + ".png", encodedLargePNG);

            textureImporter =
                AssetImporter.GetAtPath(m_TextureStreamingMipmapDisabled.relativePath) as TextureImporter;
            textureImporter.mipmapEnabled = true;
            textureImporter.streamingMipmaps = false;
            //Size should not be compressed for testing purposes.
            //If compressed, it won't trigger a warning, as size will be below the minimal size
            textureImporter.textureCompression = TextureImporterCompression.Uncompressed;
            textureImporter.SaveAndReimport();

            m_TextureStreamingMipmapEnabled =
                new TestAsset(k_TextureNameStreamingMipmapEnabled + ".png", encodedLargePNG);

            textureImporter =
                AssetImporter.GetAtPath(m_TextureStreamingMipmapEnabled.relativePath) as TextureImporter;
            textureImporter.streamingMipmaps = true;
            textureImporter.SaveAndReimport();

            m_TestTextureAnisotropicLevelBig = new TestAsset(k_TextureNameAnisotropicLevelBig + ".png", encodedPNG);
            textureImporter = AssetImporter.GetAtPath(m_TestTextureAnisotropicLevelBig.relativePath) as TextureImporter;
            textureImporter.anisoLevel = 2;
            textureImporter.SaveAndReimport();

            m_TestTextureAnisotropicLevelOne = new TestAsset(k_TextureNameAnisotropicLevelOne + ".png", encodedPNG);
            textureImporter = AssetImporter.GetAtPath(m_TestTextureAnisotropicLevelOne.relativePath) as TextureImporter;
            textureImporter.anisoLevel = 1;
            textureImporter.filterMode = FilterMode.Bilinear;
            textureImporter.mipmapEnabled = true;
            textureImporter.SaveAndReimport();

            var solidColorTexture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            solidColorTexture.SetPixel(0, 0, Color.red);
            solidColorTexture.SetPixel(1, 0, Color.red);
            solidColorTexture.SetPixel(0, 1, Color.red);
            solidColorTexture.SetPixel(1, 1, Color.red);

            var encodedSolidColorPNG = solidColorTexture.EncodeToPNG();
            m_TextureSolidColor = new TestAsset(k_TextureNameSolidColor + ".png", encodedSolidColorPNG);
            textureImporter = AssetImporter.GetAtPath(m_TextureSolidColor.relativePath) as TextureImporter;
            textureImporter.SaveAndReimport();

            var notSolidColorTexture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            notSolidColorTexture.SetPixel(0, 0, Color.blue);
            notSolidColorTexture.SetPixel(1, 0, Color.red);

            var encodedNotSolidColorPNG = notSolidColorTexture.EncodeToPNG();
            m_TextureNotSolidColor = new TestAsset(k_TextureNameNotSolidColor + ".png", encodedNotSolidColorPNG);
            textureImporter = AssetImporter.GetAtPath(m_TextureNotSolidColor.relativePath) as TextureImporter;
            textureImporter.SaveAndReimport();

            var emptyTexture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            emptyTexture.SetPixel(0, 0, new Color(1, 0, 0, 1));
            emptyTexture.SetPixel(1, 0, new Color(1, 0, 0, 0));
            emptyTexture.SetPixel(0, 1, new Color(1, 0, 0, 0));
            emptyTexture.SetPixel(1, 1, new Color(1, 0, 0, 0));

            var emptyTexturePNG = emptyTexture.EncodeToPNG();

            m_TestTextureEmptySpace = new TestAsset(k_TextureNameEmptySpace + ".png", emptyTexturePNG);
            textureImporter = AssetImporter.GetAtPath(m_TestTextureEmptySpace.relativePath) as TextureImporter;
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
            Assert.AreEqual(k_TextureName, texture.Description);

            Assert.AreEqual(textureImporter.textureShape.ToString(), texture.GetCustomProperty(TextureProperty.Shape));
            Assert.AreEqual(textureImporter.textureType.ToString(),
                texture.GetCustomProperty(TextureProperty.ImporterType));
            Assert.AreEqual(textureImporter.textureCompression.ToString(),
                texture.GetCustomProperty(TextureProperty.TextureCompression));

            Assert.AreEqual("AutomaticCompressed", texture.GetCustomProperty(TextureProperty.Format));
            Assert.True(texture.GetCustomPropertyBool(TextureProperty.MipMapEnabled));
            Assert.False(texture.GetCustomPropertyBool(TextureProperty.Readable));
            Assert.False(texture.GetCustomPropertyBool(TextureProperty.StreamingMipMap));
            Assert.AreEqual((k_Resolution + "x" + k_Resolution), texture.GetCustomProperty(TextureProperty.Resolution));

            /*
            var texture = AssetDatabase.LoadAssetAtPath<Texture>(m_TestTexture.relativePath);
            Assert.NotNull(texture);
            Assert.AreEqual(Profiler.GetRuntimeMemorySizeLong(texture).ToString(), reportedTexture.GetCustomProperty(TextureProperty.SizeOnDisk), "Checked Texture Size");
            */
        }

        [Test]
        public void Texture_MipMapUnused_IsReported()
        {
            var textureDiagnostic =
                AnalyzeAndFindAssetIssues(m_TestTextureNoMipMapDefault, IssueCategory.AssetDiagnostic)
                    .FirstOrDefault(i => i.Id.Equals(TextureAnalyzer.k_TextureMipMapNotEnabledDescriptor.Id));

            Assert.NotNull(textureDiagnostic);
        }

        [Test]
        public void Texture_MipMapUnused_IsNotReported()
        {
            var textureDiagnostic = AnalyzeAndFindAssetIssues(m_TestTextureMipMapDefault, IssueCategory.AssetDiagnostic)
                .FirstOrDefault(i => i.Id.Equals(TextureAnalyzer.k_TextureMipMapNotEnabledDescriptor.Id));

            Assert.Null(textureDiagnostic);
        }

        [Test]
        public void Texture_MipMapUsedForGUI_IsReported()
        {
            var textureDiagnostic = AnalyzeAndFindAssetIssues(m_TestTextureMipMapGui, IssueCategory.AssetDiagnostic)
                .FirstOrDefault(i => i.Id.Equals(TextureAnalyzer.k_TextureMipMapEnabledDescriptor.Id));

            Assert.NotNull(textureDiagnostic);
            var descriptor = textureDiagnostic.Id.GetDescriptor();
            Assert.NotNull(descriptor);
            Assert.NotNull(descriptor.fixer);

            descriptor.Fix(textureDiagnostic);

            textureDiagnostic = AnalyzeAndFindAssetIssues(m_TestTextureMipMapGui, IssueCategory.AssetDiagnostic)
                .FirstOrDefault(i => i.Id.Equals(TextureAnalyzer.k_TextureMipMapEnabledDescriptor.Id));

            Assert.Null(textureDiagnostic);
        }

        [Test]
        public void Texture_MipMapUsedForSprite_IsReportedAndFixed()
        {
            var textureDiagnostic = AnalyzeAndFindAssetIssues(m_TestTextureMipMapSprite, IssueCategory.AssetDiagnostic)
                .FirstOrDefault(i => i.Id.Equals(TextureAnalyzer.k_TextureMipMapEnabledDescriptor.Id));

            Assert.NotNull(textureDiagnostic);
            var descriptor = textureDiagnostic.Id.GetDescriptor();
            Assert.NotNull(descriptor);
            Assert.NotNull(descriptor.fixer);

            descriptor.Fix(textureDiagnostic);

            textureDiagnostic = AnalyzeAndFindAssetIssues(m_TestTextureMipMapSprite, IssueCategory.AssetDiagnostic)
                .FirstOrDefault(i => i.Id.Equals(TextureAnalyzer.k_TextureMipMapEnabledDescriptor.Id));

            Assert.Null(textureDiagnostic);
        }

        [Test]
        public void Texture_ReadWriteEnabled_IsReported()
        {
            var textureDiagnostic =
                AnalyzeAndFindAssetIssues(m_TestTextureReadWriteEnabled, IssueCategory.AssetDiagnostic)
                    .FirstOrDefault(i => i.Id.Equals(TextureAnalyzer.k_TextureReadWriteEnabledDescriptor.Id));

            Assert.NotNull(textureDiagnostic);
            var descriptor = textureDiagnostic.Id.GetDescriptor();
            Assert.NotNull(descriptor);
            Assert.NotNull(descriptor.fixer);

            descriptor.Fix(textureDiagnostic);

            textureDiagnostic = AnalyzeAndFindAssetIssues(m_TestTextureReadWriteEnabled, IssueCategory.AssetDiagnostic)
                .FirstOrDefault(i => i.Id.Equals(TextureAnalyzer.k_TextureReadWriteEnabledDescriptor.Id));

            Assert.Null(textureDiagnostic);
        }

        [Test]
        public void Texture_ReadWriteEnabled_IsNotReported()
        {
            var textureDiagnostic =
                AnalyzeAndFindAssetIssues(m_TestTextureNoMipMapDefault, IssueCategory.AssetDiagnostic)
                    .FirstOrDefault(i => i.Id.Equals(TextureAnalyzer.k_TextureReadWriteEnabledDescriptor.Id));

            Assert.IsNull(textureDiagnostic);
        }

        [Test]
        public void Texture_StreamingMipmapDisabled_IsReported()
        {
            var textureDiagnostic =
                AnalyzeAndFindAssetIssues(m_TextureStreamingMipmapDisabled, IssueCategory.AssetDiagnostic)
                    .FirstOrDefault(i =>
                    i.Id.Equals(TextureAnalyzer.k_TextureStreamingMipMapEnabledDescriptor.Id));

            Assert.NotNull(textureDiagnostic);
            var descriptor = textureDiagnostic.Id.GetDescriptor();
            Assert.NotNull(descriptor);
            Assert.NotNull(descriptor.fixer);

            descriptor.Fix(textureDiagnostic);

            textureDiagnostic =
                AnalyzeAndFindAssetIssues(m_TextureStreamingMipmapDisabled, IssueCategory.AssetDiagnostic)
                    .FirstOrDefault(i =>
                    i.Id.Equals(TextureAnalyzer.k_TextureStreamingMipMapEnabledDescriptor.Id));

            Assert.Null(textureDiagnostic);
        }

        [Test]
        public void Texture_StreamingMipmapEnabled_IsNotReported()
        {
            var textureDiagnostic =
                AnalyzeAndFindAssetIssues(m_TextureStreamingMipmapEnabled, IssueCategory.AssetDiagnostic)
                    .FirstOrDefault(i =>
                    i.Id.Equals(TextureAnalyzer.k_TextureStreamingMipMapEnabledDescriptor.Id));

            Assert.IsNull(textureDiagnostic);
        }

        [Test]
        [RequirePlatformSupport(BuildTarget.Android)]
        public void Texture_AnisotropicLevel_IsReported()
        {
            var oldPlatform = m_Platform;
            m_Platform = BuildTarget.Android;

            var textureDiagnostic =
                AnalyzeAndFindAssetIssues(m_TestTextureAnisotropicLevelBig, IssueCategory.AssetDiagnostic)
                    .FirstOrDefault(i => i.Id.Equals(TextureAnalyzer.k_TextureAnisotropicLevelDescriptor.Id));

            Assert.NotNull(textureDiagnostic);
            var descriptor = textureDiagnostic.Id.GetDescriptor();
            Assert.NotNull(descriptor);
            Assert.NotNull(descriptor.fixer);

            descriptor.Fix(textureDiagnostic);

            textureDiagnostic =
                AnalyzeAndFindAssetIssues(m_TestTextureAnisotropicLevelBig, IssueCategory.AssetDiagnostic)
                    .FirstOrDefault(i => i.Id.Equals(TextureAnalyzer.k_TextureAnisotropicLevelDescriptor.Id));

            Assert.Null(textureDiagnostic);

            m_Platform = oldPlatform;
        }

        [Test]
        public void Texture_AnisotropicLevel_IsNotReported()
        {
            var textureDiagnostic =
                AnalyzeAndFindAssetIssues(m_TestTextureAnisotropicLevelOne, IssueCategory.AssetDiagnostic)
                    .FirstOrDefault(i => i.Id.Equals(TextureAnalyzer.k_TextureAnisotropicLevelDescriptor.Id));

            Assert.IsNull(textureDiagnostic);

            var textureImporter =
                AssetImporter.GetAtPath(m_TestTextureAnisotropicLevelOne.relativePath) as TextureImporter;
            textureImporter.anisoLevel = 2;
            textureImporter.mipmapEnabled = false;
            textureImporter.SaveAndReimport();

            textureDiagnostic =
                AnalyzeAndFindAssetIssues(m_TestTextureAnisotropicLevelOne, IssueCategory.AssetDiagnostic)
                    .FirstOrDefault(i => i.Id.Equals(TextureAnalyzer.k_TextureAnisotropicLevelDescriptor.Id));
            Assert.IsNull(textureDiagnostic);

            textureImporter.mipmapEnabled = true;
            textureImporter.filterMode = FilterMode.Point;
            textureImporter.SaveAndReimport();

            textureDiagnostic =
                AnalyzeAndFindAssetIssues(m_TestTextureAnisotropicLevelOne, IssueCategory.AssetDiagnostic)
                    .FirstOrDefault(i => i.Id.Equals(TextureAnalyzer.k_TextureAnisotropicLevelDescriptor.Id));
            Assert.IsNull(textureDiagnostic);

            textureImporter.anisoLevel = 1;
            textureImporter.filterMode = FilterMode.Bilinear;
            textureImporter.SaveAndReimport();
        }

        [Test]
        public void Texture_SolidTexture_IsReported()
        {
            var textureDiagnostic = AnalyzeAndFindAssetIssues(m_TextureSolidColor, IssueCategory.AssetDiagnostic)
                .FirstOrDefault(i => i.Id.Equals(TextureUtilizationAnalyzer.k_TextureSolidColorDescriptor.Id));

            Assert.IsNotNull(textureDiagnostic);
        }

        [Test]
        public void Texture_Not_SolidTexture_IsNotReported()
        {
            var textureDiagnostic = AnalyzeAndFindAssetIssues(m_TextureNotSolidColor, IssueCategory.AssetDiagnostic)
                .FirstOrDefault(i => i.Id.Equals(TextureUtilizationAnalyzer.k_TextureSolidColorDescriptor.Id));

            Assert.IsNull(textureDiagnostic);
        }

        [Test]
        public void Texture_EmptySpace_IsReported()
        {
            var textureDiagnostic = AnalyzeAndFindAssetIssues(m_TestTextureEmptySpace, IssueCategory.AssetDiagnostic)
                .FirstOrDefault(i => i.Id.Equals(TextureUtilizationAnalyzer.k_TextureAtlasEmptyDescriptor.Id));

            Assert.IsNotNull(textureDiagnostic);
        }

        [Test]
        public void Texture_EmptySpace_IsNotReported()
        {
            //We don't need to create a new texture as we only need a not empty one
            var textureDiagnostic = AnalyzeAndFindAssetIssues(m_TextureSolidColor, IssueCategory.AssetDiagnostic)
                .FirstOrDefault(i => i.Id.Equals(TextureUtilizationAnalyzer.k_TextureAtlasEmptyDescriptor.Id));

            Assert.IsNull(textureDiagnostic);
        }
    }
}
