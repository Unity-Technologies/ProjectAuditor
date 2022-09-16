using NUnit.Framework;
using Unity.ProjectAuditor.Editor;
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
            texture.name = "ProceduralTextureForTest321.png";
            texture.Apply();

            var bytes = texture.EncodeToPNG();

            var tempTestTexture = new TempAsset(texture.name, bytes);

            // var allTextures = AssetDatabase.FindAssets("t: Texture, a:assets");

            //  textureToCompare = (Texture)AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(allTextures[0]), typeof(Texture));
            textureViaImporter = (AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(texture)) as TextureImporter); //Needed since the texture/texture via TextureImporter properties are both used in the TextureModule script to access varying properties.
        }

        [Test]
        [Explicit]
        public void Texture_Properties_AreReported()
        {
            var textureTests = Analyze(IssueCategory.Texture);

            Assert.AreEqual((AssetDatabase.GetAssetPath(texture)), textureTests[0].customProperties[0], "Checked Texture Name");

            Assert.AreEqual(textureViaImporter.textureShape.ToString(), textureTests[0].customProperties[1], "Checked Texture Shape/Dimension");

            Assert.AreEqual(textureViaImporter.textureType.ToString(), textureTests[0].customProperties[2], "Checked TextureImporterType ");

            Assert.AreEqual(texture.format, textureTests[0].customProperties[3], "Checked Texture Format");

            Assert.AreEqual(textureViaImporter.textureCompression.ToString(), textureTests[0].customProperties[4], "Checked Texture Compression");

            Assert.AreEqual(textureViaImporter.mipmapEnabled, textureTests[0].customProperties[5], "Checked MipMaps Enabled");

            Assert.AreEqual(textureViaImporter.isReadable, textureTests[0].customProperties[6], "Checked Texture Read/Write");

            Assert.AreEqual((resolution + "x" + resolution), textureTests[0].customProperties[7], "Checked Texture Resolution");

            Assert.AreEqual(Profiler.GetRuntimeMemorySizeLong(texture), textureTests[0].customProperties[8], "Checked Texture Size");

            Assert.AreEqual(currentPlatform, textureTests[0].customProperties[9], "Checked Platform");
        }
    }
}
