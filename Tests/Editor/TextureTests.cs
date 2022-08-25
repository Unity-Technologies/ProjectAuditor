using NUnit.Framework;
using Unity.ProjectAuditor.Editor;
using System.IO;
using Unity.ProjectAuditor.EditorTests;
using UnityEditor;
using UnityEngine;


namespace Unity.ProjectAuditor.EditorTests
{
    class TextureTests : TestFixtureBase
    {
        public int projectTextureCount;
        public int resolution = 1;

        [OneTimeSetUp]
        public void SetUp()
        {
            var fullFilePath = Application.dataPath + "/Textures/" + "ProceduralTextureForTest321.png";
            Texture2D texture;

            texture = new Texture2D(resolution, resolution); //defaults: mipmaps = true & format = automatic
            texture.SetPixel(0, 0, Random.ColorHSV());
            texture.name = "ProceduralTextureForTest321";
            texture.Apply();

            byte[] bytes = texture.EncodeToPNG();

            File.WriteAllBytes(fullFilePath, bytes);
            AssetDatabase.Refresh();
            projectTextureCount += 1;

            projectTextureCount = AssetDatabase.FindAssets("t: Texture, a:assets").Length;
        }

        [Test]
        public void Texture_Properties_AreReported()
        {
            var TextureTests = Analyze(IssueCategory.Texture);

            Assert.AreEqual(projectTextureCount, TextureTests.Length, "Checked Texture Count");

            Assert.AreEqual("ProceduralTextureForTest321", TextureTests[0].customProperties[0], "Checked Texture Name");

            Assert.AreEqual("Image", TextureTests[0].customProperties[2], "Checked TextureImporterType "); // Shown as "Default" in Editor but corresponds as "Image" (value returned) in API

            Assert.AreEqual("AutomaticCompressed", TextureTests[0].customProperties[3], "Checked Texture Compression");

            Assert.AreEqual("True", TextureTests[0].customProperties[5], "Checked MipMaps Enabled");

            Assert.AreEqual("False", TextureTests[0].customProperties[6], "Checked Texture Read/Write");

            Assert.AreEqual((resolution + "x" + resolution).ToString(), TextureTests[0].customProperties[7], "Checked Texture Resolution");
        }
    }
}
