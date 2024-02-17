using System.Linq;
using NUnit.Framework;
using Unity.ProjectAuditor.Editor;
using Unity.ProjectAuditor.Editor.Modules;
using Unity.ProjectAuditor.Editor.Tests.Common;
using UnityEditor;
using UnityEditor.U2D;
using UnityEngine;

namespace Unity.ProjectAuditor.EditorTests
{
    class SpriteAtlasTests : TestFixtureBase
    {
        const string k_SpriteAtlasName = "SpriteAtlasForTest";
        const string k_SpriteAtlasNameFull = k_SpriteAtlasName + "Full";
        const string k_SpriteAtlasNameEmpty = k_SpriteAtlasName + "Empty";

        const string k_BlueSquareSprite = "BlueSquareSprite";
        const string k_RedSquareSprite = "RedSquareSprite";
        const string k_EmptySquareSprite = "EmptySquareSprite";

        TestAsset m_TestSpriteAtlasFull;
        TestAsset m_TestSpriteAtlasEmpty;

        TestAsset m_RedSquareSprite;
        TestAsset m_BlueSquareSprite;
        TestAsset m_EmptySquareSprite;

        SpritePackerMode m_SpritePackerMode;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            m_SpritePackerMode = EditorSettings.spritePackerMode;
            EditorSettings.spritePackerMode = SpritePackerMode.SpriteAtlasV2;

            //Full Sprite Atlas Generation
            var fullSpriteAtlasAsset = new SpriteAtlasAsset();
            fullSpriteAtlasAsset.name = k_SpriteAtlasNameFull;

            GenerateTestSpritesForFullSpriteAtlasTest();

            var blueSquareTextureImporter = AssetImporter.GetAtPath(m_BlueSquareSprite.RelativePath) as TextureImporter;
            blueSquareTextureImporter.textureType = TextureImporterType.Sprite;
            blueSquareTextureImporter.SaveAndReimport();

            var blueSquareSprite = AssetDatabase.LoadAssetAtPath<Sprite>(m_BlueSquareSprite.RelativePath);

            var redSquareTextureImporter = AssetImporter.GetAtPath(m_RedSquareSprite.RelativePath) as TextureImporter;
            redSquareTextureImporter.textureType = TextureImporterType.Sprite;
            redSquareTextureImporter.SaveAndReimport();

            var redSquareSprite = AssetDatabase.LoadAssetAtPath<Sprite>(m_RedSquareSprite.RelativePath);

            fullSpriteAtlasAsset.Add(new Object[] {blueSquareSprite, redSquareSprite});
            m_TestSpriteAtlasFull = TestAsset.SaveSpriteAtlasAsset(fullSpriteAtlasAsset, k_SpriteAtlasNameFull + ".spriteatlasv2");

            //Empty Sprite Atlas Generation
            var emptySpriteAtlasAsset = new SpriteAtlasAsset();
            emptySpriteAtlasAsset.name = k_SpriteAtlasNameEmpty;

            GenerateTestSpritesForEmptySpriteAtlasTest();

            var emptySquareTextureImporter = AssetImporter.GetAtPath(m_EmptySquareSprite.RelativePath) as TextureImporter;
            emptySquareTextureImporter.textureType = TextureImporterType.Sprite;
            emptySquareTextureImporter.SaveAndReimport();

            var emptySquareSprite = AssetDatabase.LoadAssetAtPath<Sprite>(m_EmptySquareSprite.RelativePath);

            emptySpriteAtlasAsset.Add(new Object[] {emptySquareSprite, emptySquareSprite});
            m_TestSpriteAtlasEmpty = TestAsset.SaveSpriteAtlasAsset(emptySpriteAtlasAsset, k_SpriteAtlasNameEmpty + ".spriteatlasv2");
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            EditorSettings.spritePackerMode = m_SpritePackerMode;
        }

        [Test]
        public void SpriteAtlas_PoorUtilization_IsDisabledByDefault()
        {
            m_AdditionalRules.Clear();

            var diagnostic =
                AnalyzeAndFindAssetIssues(m_TestSpriteAtlasEmpty, IssueCategory.AssetIssue)
                    .FirstOrDefault(i => i.Id.Equals(SpriteAtlasAnalyzer.k_PoorUtilizationDescriptor.Id));

            Assert.IsNull(diagnostic);
        }

        [Test]
#if UNITY_2023_1_OR_NEWER
        [Ignore("TODO: investigate reason for test failure")]
#endif
        public void SpriteAtlas_PoorUtilization_IsNotReported()
        {
            // enable diagnostic rule since it is disabled by default
            m_AdditionalRules.Add(new Rule
            {
                Id = SpriteAtlasAnalyzer.k_PoorUtilizationDescriptor.Id,
                Severity = Severity.Moderate
            });

            var diagnostic =
                AnalyzeAndFindAssetIssues(m_TestSpriteAtlasFull, IssueCategory.AssetIssue)
                    .FirstOrDefault(i => i.Id.Equals(SpriteAtlasAnalyzer.k_PoorUtilizationDescriptor.Id));

            Assert.Null(diagnostic);
        }

        [Test]
#if UNITY_2023_1_OR_NEWER
        [Ignore("TODO: investigate reason for test failure")]
#endif
        public void SpriteAtlas_PoorUtilization_IsReported()
        {
            // enable diagnostic rule since it is disabled by default
            m_AdditionalRules.Add(new Rule
            {
                Id = SpriteAtlasAnalyzer.k_PoorUtilizationDescriptor.Id,
                Severity = Severity.Moderate
            });

            var diagnostic =
                AnalyzeAndFindAssetIssues(m_TestSpriteAtlasEmpty, IssueCategory.AssetIssue)
                    .FirstOrDefault(i => i.Id.Equals(SpriteAtlasAnalyzer.k_PoorUtilizationDescriptor.Id));

            Assert.IsNotNull(diagnostic);
        }

        void GenerateTestSpritesForFullSpriteAtlasTest()
        {
            var blueSquareTexture = new Texture2D(25, 25);
            var redSquareTexture = new Texture2D(25, 25);

            var length = blueSquareTexture.GetPixels().Length;

            for (var x = 0; x < length; x++)
            {
                for (var y = 0; y < length; y++)
                {
                    blueSquareTexture.SetPixel(x, y, Color.blue);
                    redSquareTexture.SetPixel(x, y, Color.red);
                }
            }

            blueSquareTexture.Apply();
            redSquareTexture.Apply();

            m_BlueSquareSprite = new TestAsset(k_BlueSquareSprite + ".png", blueSquareTexture.EncodeToPNG());
            m_RedSquareSprite = new TestAsset(k_RedSquareSprite + ".png", redSquareTexture.EncodeToPNG());
        }

        void GenerateTestSpritesForEmptySpriteAtlasTest()
        {
            var emptySquareTexture = new Texture2D(25, 25);
            var length = emptySquareTexture.GetPixels().Length;

            for (var x = 0; x < length; x++)
            {
                for (var y = 0; y < length; y++)
                {
                    emptySquareTexture.SetPixel(x, y, new Color(0, 0, 0, 0));
                }
            }

            emptySquareTexture.SetPixel(0, 0, Random.ColorHSV());
            emptySquareTexture.Apply();

            m_EmptySquareSprite = new TestAsset(k_EmptySquareSprite + ".png", emptySquareTexture.EncodeToPNG());
        }
    }
}
