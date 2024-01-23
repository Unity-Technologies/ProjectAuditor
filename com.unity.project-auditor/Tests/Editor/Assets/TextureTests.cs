using System.Linq;
using NUnit.Framework;
using Unity.ProjectAuditor.Editor;
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
        const string k_TextureNameMipmapsDefault = k_TextureName + "MipMapDefaultTest1234";
        const string k_TextureNameNoMipmapsDefault = k_TextureName + "NoMipmapsDefaultTest1234";
        const string k_TextureNameMipmapsGUI = k_TextureName + "MipMapGUITest1234";
        const string k_TextureNameMipmapsSprite = k_TextureName + "MipMapSpriteTest1234";
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
        TestAsset m_TestTextureMipmapsDefault;
        TestAsset m_TestTextureNoMipmapsDefault;
        TestAsset m_TestTextureMipmapsGui;
        TestAsset m_TestTextureMipmapsSprite;
        TestAsset m_TestTextureReadWriteEnabled;
        TestAsset m_TextureStreamingMipmapDisabled;
        TestAsset m_TextureStreamingMipmapEnabled;
        TestAsset m_TestTextureAnisotropicEnabled;
        TestAsset m_TestTextureAnisotropicDisabled;
        TestAsset m_TextureSolidColor;
        TestAsset m_TextureNotSolidColor;
        TestAsset m_TestTextureEmptySpace;

        [OneTimeSetUp]
        public void OneTimeSetUp()
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

            m_TestTextureMipmapsDefault = new TestAsset(k_TextureNameMipmapsDefault + ".png", encodedPNG);

            var textureImporter = AssetImporter.GetAtPath(m_TestTextureMipmapsDefault.RelativePath) as TextureImporter;
            textureImporter.textureType = TextureImporterType.Default;
            textureImporter.mipmapEnabled = true;
            textureImporter.SaveAndReimport();

            m_TestTextureNoMipmapsDefault = new TestAsset(k_TextureNameNoMipmapsDefault + ".png", encodedPNG);

            textureImporter = AssetImporter.GetAtPath(m_TestTextureNoMipmapsDefault.RelativePath) as TextureImporter;
            textureImporter.textureType = TextureImporterType.Default;
            textureImporter.mipmapEnabled = false;
            textureImporter.SaveAndReimport();

            m_TestTextureMipmapsGui = new TestAsset(k_TextureNameMipmapsGUI + ".png", encodedPNG);

            textureImporter = AssetImporter.GetAtPath(m_TestTextureMipmapsGui.RelativePath) as TextureImporter;
            textureImporter.textureType = TextureImporterType.GUI;
            textureImporter.mipmapEnabled = true;
            textureImporter.SaveAndReimport();

            m_TestTextureMipmapsSprite = new TestAsset(k_TextureNameMipmapsSprite + ".png", encodedPNG);

            textureImporter = AssetImporter.GetAtPath(m_TestTextureMipmapsSprite.RelativePath) as TextureImporter;
            textureImporter.textureType = TextureImporterType.Sprite;
            textureImporter.mipmapEnabled = true;
            textureImporter.SaveAndReimport();

            m_TestTextureReadWriteEnabled = new TestAsset(k_TextureNameReadWriteEnabled + ".png", encodedPNG);

            textureImporter = AssetImporter.GetAtPath(m_TestTextureReadWriteEnabled.RelativePath) as TextureImporter;
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
                AssetImporter.GetAtPath(m_TextureStreamingMipmapDisabled.RelativePath) as TextureImporter;
            textureImporter.mipmapEnabled = true;
            textureImporter.streamingMipmaps = false;
            //Size should not be compressed for testing purposes.
            //If compressed, it won't trigger a warning, as size will be below the minimal size
            textureImporter.textureCompression = TextureImporterCompression.Uncompressed;
            textureImporter.SaveAndReimport();

            m_TextureStreamingMipmapEnabled =
                new TestAsset(k_TextureNameStreamingMipmapEnabled + ".png", encodedLargePNG);

            textureImporter =
                AssetImporter.GetAtPath(m_TextureStreamingMipmapEnabled.RelativePath) as TextureImporter;
            textureImporter.streamingMipmaps = true;
            textureImporter.SaveAndReimport();

            m_TestTextureAnisotropicEnabled = new TestAsset(k_TextureNameAnisotropicLevelBig + ".png", encodedPNG);
            textureImporter = AssetImporter.GetAtPath(m_TestTextureAnisotropicEnabled.RelativePath) as TextureImporter;
            textureImporter.anisoLevel = 2;
            textureImporter.SaveAndReimport();

            m_TestTextureAnisotropicDisabled = new TestAsset(k_TextureNameAnisotropicLevelOne + ".png", encodedPNG);
            textureImporter = AssetImporter.GetAtPath(m_TestTextureAnisotropicDisabled.RelativePath) as TextureImporter;
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
            textureImporter = AssetImporter.GetAtPath(m_TextureSolidColor.RelativePath) as TextureImporter;
            textureImporter.SaveAndReimport();

            var notSolidColorTexture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            notSolidColorTexture.SetPixel(0, 0, Color.blue);
            notSolidColorTexture.SetPixel(1, 0, Color.red);

            var encodedNotSolidColorPNG = notSolidColorTexture.EncodeToPNG();
            m_TextureNotSolidColor = new TestAsset(k_TextureNameNotSolidColor + ".png", encodedNotSolidColorPNG);
            textureImporter = AssetImporter.GetAtPath(m_TextureNotSolidColor.RelativePath) as TextureImporter;
            textureImporter.SaveAndReimport();

            var emptyTexture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            emptyTexture.SetPixel(0, 0, new Color(1, 0, 0, 1));
            emptyTexture.SetPixel(1, 0, new Color(1, 0, 0, 0));
            emptyTexture.SetPixel(0, 1, new Color(1, 0, 0, 0));
            emptyTexture.SetPixel(1, 1, new Color(1, 0, 0, 0));

            var emptyTexturePNG = emptyTexture.EncodeToPNG();

            m_TestTextureEmptySpace = new TestAsset(k_TextureNameEmptySpace + ".png", emptyTexturePNG);
            textureImporter = AssetImporter.GetAtPath(m_TestTextureEmptySpace.RelativePath) as TextureImporter;
            textureImporter.SaveAndReimport();

            AnalyzeTestAssets();
        }

        [TearDown]
        public void TearDown()
        {
            // a few tests change m_Platform so we need to restore it
            m_Platform = GetDefaultBuildTarget();
        }

        [Test]
        public void Texture_Properties_AreReported()
        {
            var textureImporter = (AssetImporter.GetAtPath(m_TestTexture.RelativePath) as TextureImporter);

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
        public void Texture_MipmapsDisabled_IsReported()
        {
            var textureDiagnostic =
                GetIssuesForAsset(m_TestTextureNoMipmapsDefault)
                    .FirstOrDefault(i => i.Id.Equals(TextureAnalyzer.k_TextureMipmapsNotEnabledDescriptor.Id));

            Assert.NotNull(textureDiagnostic);
        }

        [Test]
        public void Texture_MipmapsDisabled_IsNotReported()
        {
            var textureDiagnostic = GetIssuesForAsset(m_TestTextureMipmapsDefault)
                .FirstOrDefault(i => i.Id.Equals(TextureAnalyzer.k_TextureMipmapsNotEnabledDescriptor.Id));

            Assert.Null(textureDiagnostic);
        }

        [Test]
        public void Texture_MipmapsEnabledForGUI_IsReported()
        {
            var textureDiagnostic = GetIssuesForAsset(m_TestTextureMipmapsGui)
                .FirstOrDefault(i => i.Id.Equals(TextureAnalyzer.k_TextureMipmapsEnabledDescriptor.Id));

            Assert.NotNull(textureDiagnostic);
            var descriptor = textureDiagnostic.Id.GetDescriptor();
            Assert.NotNull(descriptor);
            Assert.NotNull(descriptor.Fixer);

            descriptor.Fix(textureDiagnostic, m_AnalysisParams);

            textureDiagnostic = AnalyzeAndFindAssetIssues(m_TestTextureMipmapsGui, IssueCategory.AssetIssue)
                .FirstOrDefault(i => i.Id.Equals(TextureAnalyzer.k_TextureMipmapsEnabledDescriptor.Id));

            Assert.Null(textureDiagnostic);
        }

        [Test]
        public void Texture_MipmapsEnabledForSprite_IsReportedAndFixed()
        {
            var textureDiagnostic = GetIssuesForAsset(m_TestTextureMipmapsSprite)
                .FirstOrDefault(i => i.Id.Equals(TextureAnalyzer.k_TextureMipmapsEnabledDescriptor.Id));

            Assert.NotNull(textureDiagnostic);
            var descriptor = textureDiagnostic.Id.GetDescriptor();
            Assert.NotNull(descriptor);
            Assert.NotNull(descriptor.Fixer);

            descriptor.Fix(textureDiagnostic, m_AnalysisParams);

            textureDiagnostic = AnalyzeAndFindAssetIssues(m_TestTextureMipmapsSprite, IssueCategory.AssetIssue)
                .FirstOrDefault(i => i.Id.Equals(TextureAnalyzer.k_TextureMipmapsEnabledDescriptor.Id));

            Assert.Null(textureDiagnostic);
        }

        [Test]
        public void Texture_ReadWriteEnabled_IsReported()
        {
            var textureDiagnostic =
                GetIssuesForAsset(m_TestTextureReadWriteEnabled)
                    .FirstOrDefault(i => i.Id.Equals(TextureAnalyzer.k_TextureReadWriteEnabledDescriptor.Id));

            Assert.NotNull(textureDiagnostic);
            var descriptor = textureDiagnostic.Id.GetDescriptor();
            Assert.NotNull(descriptor);
            Assert.NotNull(descriptor.Fixer);

            descriptor.Fix(textureDiagnostic, m_AnalysisParams);

            textureDiagnostic = AnalyzeAndFindAssetIssues(m_TestTextureReadWriteEnabled, IssueCategory.AssetIssue)
                .FirstOrDefault(i => i.Id.Equals(TextureAnalyzer.k_TextureReadWriteEnabledDescriptor.Id));

            Assert.Null(textureDiagnostic);
        }

        [Test]
        public void Texture_ReadWriteEnabled_IsNotReported()
        {
            var textureDiagnostic =
                GetIssuesForAsset(m_TestTextureNoMipmapsDefault)
                    .FirstOrDefault(i => i.Id.Equals(TextureAnalyzer.k_TextureReadWriteEnabledDescriptor.Id));

            Assert.IsNull(textureDiagnostic);
        }

        [Test]
        public void Texture_StreamingMipmapDisabled_IsReported()
        {
            var textureDiagnostic =
                GetIssuesForAsset(m_TextureStreamingMipmapDisabled)
                    .FirstOrDefault(i =>
                    i.Id.Equals(TextureAnalyzer.k_TextureStreamingMipMapEnabledDescriptor.Id));

            Assert.NotNull(textureDiagnostic);
            var descriptor = textureDiagnostic.Id.GetDescriptor();
            Assert.NotNull(descriptor);
            Assert.NotNull(descriptor.Fixer);

            descriptor.Fix(textureDiagnostic, m_AnalysisParams);

            textureDiagnostic =
                AnalyzeAndFindAssetIssues(m_TextureStreamingMipmapDisabled, IssueCategory.AssetIssue)
                    .FirstOrDefault(i =>
                    i.Id.Equals(TextureAnalyzer.k_TextureStreamingMipMapEnabledDescriptor.Id));

            Assert.Null(textureDiagnostic);
        }

        [Test]
        public void Texture_StreamingMipmapEnabled_IsNotReported()
        {
            var textureDiagnostic =
                GetIssuesForAsset(m_TextureStreamingMipmapEnabled)
                    .FirstOrDefault(i =>
                    i.Id.Equals(TextureAnalyzer.k_TextureStreamingMipMapEnabledDescriptor.Id));

            Assert.IsNull(textureDiagnostic);
        }

        [Test]
        [RequirePlatformSupport(BuildTarget.Android)]
        public void Texture_AnisotropicLevel_IsReported()
        {
            m_Platform = BuildTarget.Android;

            var textureDiagnostic =
                AnalyzeAndFindAssetIssues(m_TestTextureAnisotropicEnabled, IssueCategory.AssetIssue)
                    .FirstOrDefault(i => i.Id.Equals(TextureAnalyzer.k_TextureAnisotropicLevelDescriptor.Id));

            Assert.NotNull(textureDiagnostic);
            var descriptor = textureDiagnostic.Id.GetDescriptor();
            Assert.NotNull(descriptor);
            Assert.NotNull(descriptor.Fixer);

            descriptor.Fix(textureDiagnostic, m_AnalysisParams);

            textureDiagnostic =
                AnalyzeAndFindAssetIssues(m_TestTextureAnisotropicEnabled, IssueCategory.AssetIssue)
                    .FirstOrDefault(i => i.Id.Equals(TextureAnalyzer.k_TextureAnisotropicLevelDescriptor.Id));

            Assert.Null(textureDiagnostic);
        }

        [Test]
        [RequirePlatformSupport(BuildTarget.Android)]
        public void Texture_AnisotropicLevel_IsNotReported()
        {
            // check previously reported issues on non-mobile platform
            var textureDiagnostic =
                GetIssuesForAsset(m_TestTextureAnisotropicEnabled)
                    .FirstOrDefault(i => i.Id.Equals(TextureAnalyzer.k_TextureAnisotropicLevelDescriptor.Id));
            Assert.IsNull(textureDiagnostic);

            m_Platform = BuildTarget.Android;

            textureDiagnostic =
                AnalyzeAndFindAssetIssues(m_TestTextureAnisotropicDisabled)
                    .FirstOrDefault(i => i.Id.Equals(TextureAnalyzer.k_TextureAnisotropicLevelDescriptor.Id));

            Assert.IsNull(textureDiagnostic);

            // Disable mipmaps on texture to test that the analyzer doesn't report an issue
            var textureImporter =
                AssetImporter.GetAtPath(m_TestTextureAnisotropicEnabled.RelativePath) as TextureImporter;
            textureImporter.mipmapEnabled = false;
            textureImporter.SaveAndReimport();

            textureDiagnostic =
                AnalyzeAndFindAssetIssues(m_TestTextureAnisotropicEnabled, IssueCategory.AssetIssue)
                    .FirstOrDefault(i => i.Id.Equals(TextureAnalyzer.k_TextureAnisotropicLevelDescriptor.Id));
            Assert.IsNull(textureDiagnostic);

            // Set texture filter mode to Point to test that the analyzer doesn't report an issue
            textureImporter.mipmapEnabled = true;
            textureImporter.filterMode = FilterMode.Point;
            textureImporter.SaveAndReimport();

            textureDiagnostic =
                AnalyzeAndFindAssetIssues(m_TestTextureAnisotropicEnabled, IssueCategory.AssetIssue)
                    .FirstOrDefault(i => i.Id.Equals(TextureAnalyzer.k_TextureAnisotropicLevelDescriptor.Id));
            Assert.IsNull(textureDiagnostic);

            // Restore texture importer settings
            textureImporter.anisoLevel = 2;
            textureImporter.filterMode = FilterMode.Bilinear;
            textureImporter.SaveAndReimport();
        }

        [Test]
        public void Texture_SolidTexture_IsReported()
        {
            var textureDiagnostic = GetIssuesForAsset(m_TextureSolidColor)
                .FirstOrDefault(i => i.Id.Equals(TextureUtilizationAnalyzer.k_TextureSolidColorDescriptor.Id));

            Assert.IsNotNull(textureDiagnostic);
        }

        [Test]
        public void Texture_SolidTexture_IsNotReported()
        {
            var textureDiagnostic = GetIssuesForAsset(m_TextureNotSolidColor)
                .FirstOrDefault(i => i.Id.Equals(TextureUtilizationAnalyzer.k_TextureSolidColorDescriptor.Id));

            Assert.IsNull(textureDiagnostic);
        }

        [Test]
        public void Texture_EmptySpace_IsReported()
        {
            var textureDiagnostic = GetIssuesForAsset(m_TestTextureEmptySpace)
                .FirstOrDefault(i => i.Id.Equals(TextureUtilizationAnalyzer.k_TextureAtlasEmptyDescriptor.Id));

            Assert.IsNotNull(textureDiagnostic);
        }

        [Test]
        public void Texture_EmptySpace_IsNotReported()
        {
            //We don't need to create a new texture as we only need a not empty one
            var textureDiagnostic = GetIssuesForAsset(m_TextureSolidColor)
                .FirstOrDefault(i => i.Id.Equals(TextureUtilizationAnalyzer.k_TextureAtlasEmptyDescriptor.Id));

            Assert.IsNull(textureDiagnostic);
        }
    }
}
