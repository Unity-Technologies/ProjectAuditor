using NUnit.Framework;
using Unity.ProjectAuditor.Editor;
using System.IO;
using System.Text;
using Unity.ProjectAuditor.EditorTests;
using UnityEditor;
using UnityEngine;


namespace Unity.ProjectAuditor.EditorTests
{
    class TextureTests : TestFixtureBase
    {
        const int resolution = 1;
        string currentplatform = EditorUserBuildSettings.activeBuildTarget.ToString();

        [OneTimeSetUp]
        public void SetUp()
        {
            var texture = new Texture2D(resolution, resolution); //defaults: mipmaps = true & format = automatic
            texture.SetPixel(0, 0, Random.ColorHSV());
            texture.name = "ProceduralTextureForTest321.png";
            texture.Apply();

            byte[] bytes = texture.EncodeToPNG();

            var tempTestTexture = new TempAsset(texture.name, bytes);

            var allTextures = AssetDatabase.FindAssets("t: Texture, a:assets");
        }

        [Test]
        [Explicit]
        public void Texture_Properties_AreReported()
        {
            var textureTests = Analyze(IssueCategory.Texture);

            Assert.AreEqual(currentplatform, textureTests[0].customProperties[9], "Checked Platform");

            Assert.AreEqual("ProceduralTextureForTest321", textureTests[0].customProperties[0], "Checked Texture Name");

            Assert.AreEqual("Image", textureTests[0].customProperties[2], "Checked TextureImporterType "); // Shown as "Default" in Editor but corresponds as "Image" (value returned) in API

            Assert.AreEqual("AutomaticCompressed", textureTests[0].customProperties[3], "Checked Texture Compression");

            Assert.AreEqual("True", textureTests[0].customProperties[5], "Checked MipMaps Enabled");

            Assert.AreEqual("False", textureTests[0].customProperties[6], "Checked Texture Read/Write");

            Assert.AreEqual((resolution + "x" + resolution).ToString(), textureTests[0].customProperties[7], "Checked Texture Resolution");
        }
    }
}
