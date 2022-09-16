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
        const int resolution = 1;
        string currentPlatform = EditorUserBuildSettings.activeBuildTarget.ToString();
        Texture2D texture;
        TextureImporter textureViaImporter;

        [OneTimeSetUp]
        public void SetUp()
        {
            texture = new Texture2D(resolution, resolution); //defaults: mipmaps = true & format = automatic
            texture.SetPixel(0, 0, Random.ColorHSV());
            texture.name = "ProceduralTextureForTest321";
            texture.Apply();
            var tempTestTexture = new TempAsset(texture.name + ".png", texture.EncodeToPNG());

            textureViaImporter = (AssetImporter.GetAtPath(tempTestTexture.relativePath) as TextureImporter); //Needed since the texture/texture via TextureImporter properties are both used in the TextureModule script to access varying properties.
        }

        [Test]
        [Explicit]
        public void Texture_Properties_AreReported()
        {
            var textureTests = Analyze(IssueCategory.Texture);

            //  Debug.Log("Names of Textures compared are: " + texture.name + " ~ & ~ " + textureTests[0].GetCustomProperty(TextureProperties.Name) + "/n"
            //      + " & their respective paths are: " + texture) + " ~ & ~ " + textureTests[0].relativePath);

            Assert.AreEqual(texture.name, textureTests[0].GetCustomProperty(TextureProperties.Name), "Checked Texture Name");

            Assert.AreEqual(textureViaImporter.textureShape.ToString(), textureTests[0].GetCustomProperty(TextureProperties.Shape), "Checked Texture Shape/Dimension");

            Assert.AreEqual(textureViaImporter.textureType.ToString(), textureTests[0].GetCustomProperty(TextureProperties.ImporterType), "Checked TextureImporterType ");

            Assert.AreEqual("AutomaticCompressed", textureTests[0].GetCustomProperty(TextureProperties.Format), "Checked Texture Format");

            Assert.AreEqual(textureViaImporter.textureCompression.ToString(), textureTests[0].GetCustomProperty(TextureProperties.TextureCompression), "Checked Texture Compression");

            Assert.AreEqual("True", textureTests[0].GetCustomProperty(TextureProperties.MipMapEnabled), "Checked MipMaps Enabled");

            Assert.AreEqual("False", textureTests[0].GetCustomProperty(TextureProperties.Readable), "Checked Texture Read/Write");

            Assert.AreEqual((resolution + "x" + resolution), textureTests[0].GetCustomProperty(TextureProperties.Resolution), "Checked Texture Resolution");

            Assert.AreEqual(Profiler.GetRuntimeMemorySizeLong(texture), textureTests[0].GetCustomProperty(TextureProperties.SizeOnDisk), "Checked Texture Size");

            Assert.AreEqual(currentPlatform, textureTests[0].GetCustomProperty(TextureProperties.Platform), "Checked Platform");
        }
    }
}
