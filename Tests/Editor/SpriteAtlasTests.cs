using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using Unity.ProjectAuditor.Editor;
using Unity.ProjectAuditor.Editor.Modules;
using Unity.ProjectAuditor.Editor.Tests.Common;
using UnityEditor;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.U2D;

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

        [OneTimeSetUp]
        public void SetUp()
        {
            //Full Sprite Atlas Generation
            var fullSpriteAtlasAsset = new SpriteAtlasAsset();
            fullSpriteAtlasAsset.name = k_SpriteAtlasNameFull;

            GenerateTestSpritesForFullSpriteAtlasTest();

            var blueSquareTextureImporter = AssetImporter.GetAtPath(m_BlueSquareSprite.relativePath) as TextureImporter;
            blueSquareTextureImporter.textureType = TextureImporterType.Sprite;
            blueSquareTextureImporter.SaveAndReimport();

            Sprite blueSquareSprite = AssetDatabase.LoadAssetAtPath<Sprite>(m_BlueSquareSprite.relativePath);

            var redSquareTextureImporter = AssetImporter.GetAtPath(m_RedSquareSprite.relativePath) as TextureImporter;
            redSquareTextureImporter.textureType = TextureImporterType.Sprite;
            redSquareTextureImporter.SaveAndReimport();

            Sprite redSquareSprite = AssetDatabase.LoadAssetAtPath<Sprite>(m_RedSquareSprite.relativePath);

            fullSpriteAtlasAsset.Add(new Object[] {blueSquareSprite, redSquareSprite});
            m_TestSpriteAtlasFull = TestAsset.SaveSpriteAtlasAsset(fullSpriteAtlasAsset, k_SpriteAtlasNameFull + ".spriteatlasv2");

            //Empty Sprite Atlas Generation
            var emptySpriteAtlasAsset = new SpriteAtlasAsset();
            emptySpriteAtlasAsset.name = k_SpriteAtlasNameEmpty;

            GenerateTestSpritesForEmptySpriteAtlasTest();

            var emptySquareTextureImporter = AssetImporter.GetAtPath(m_EmptySquareSprite.relativePath) as TextureImporter;
            emptySquareTextureImporter.textureType = TextureImporterType.Sprite;
            emptySquareTextureImporter.SaveAndReimport();

            Sprite emptySquareSprite = AssetDatabase.LoadAssetAtPath<Sprite>(m_EmptySquareSprite.relativePath);


            emptySpriteAtlasAsset.Add(new Object[] {emptySquareSprite, emptySquareSprite});
            m_TestSpriteAtlasEmpty = TestAsset.SaveSpriteAtlasAsset(emptySpriteAtlasAsset, k_SpriteAtlasNameEmpty + ".spriteatlasv2");
        }

        //SpriteAtlasAsset does not exist before Unity 2020
        [Test]
        [Ignore("TODO: investigate reason for test failure")]
        public void SpriteAtlas_Not_Empty_Is_Not_Reported()
        {
            var textureDiagnostic =
                AnalyzeAndFindAssetIssues(m_TestSpriteAtlasFull, IssueCategory.AssetDiagnostic)
                    .FirstOrDefault(i => i.Id.Equals(SpriteAtlasAnalyzer.k_SpriteAtlasEmptyDescriptor.Id));

            Assert.Null(textureDiagnostic);
        }

        [Test]
        [Ignore("TODO: investigate reason for test failure")]
        public void SpriteAtlas_Empty_Is_Reported()
        {
            var textureDiagnostic =
                AnalyzeAndFindAssetIssues(m_TestSpriteAtlasEmpty, IssueCategory.AssetDiagnostic)
                    .FirstOrDefault(i => i.Id.Equals(SpriteAtlasAnalyzer.k_SpriteAtlasEmptyDescriptor.Id));

            Assert.IsNotNull(textureDiagnostic);
        }

        void GenerateTestSpritesForFullSpriteAtlasTest()
        {
            var blueSquaretexture = new Texture2D(25, 25);
            var redSquaretexture = new Texture2D(25, 25);

            int length = blueSquaretexture.GetPixels().Length;

            for (int x = 0; x < length; x++)
            {
                for (int y = 0; y < length; y++)
                {
                    blueSquaretexture.SetPixel(x, y, Color.blue);
                    redSquaretexture.SetPixel(x, y, Color.red);
                }
            }

            blueSquaretexture.Apply();
            redSquaretexture.Apply();

            m_BlueSquareSprite = new TestAsset(k_BlueSquareSprite + ".png", blueSquaretexture.EncodeToPNG());
            m_RedSquareSprite = new TestAsset(k_RedSquareSprite + ".png", redSquaretexture.EncodeToPNG());
        }

        void GenerateTestSpritesForEmptySpriteAtlasTest()
        {
            var emptySquareTexture = new Texture2D(25, 25);
            int length = emptySquareTexture.GetPixels().Length;

            for (int x = 0; x < length; x++)
            {
                for (int y = 0; y < length; y++)
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
