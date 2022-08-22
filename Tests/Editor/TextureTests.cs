using NUnit.Framework;
using Unity.ProjectAuditor.Editor;
using System.IO;
using Unity.ProjectAuditor.EditorTests;
using UnityEditor;
using UnityEngine;


namespace unity.ProjectAuditor.EditorTests
{
    class TextureTests : TestFixtureBase
    {
        public int projectTextureCount;
        public int resolution = 1;

        [OneTimeSetUp]
        public void SetUp()
        {
            projectTextureCount = AssetDatabase.FindAssets("t: Texture, a:assets").Length;

            string fullFilePath = Application.dataPath + "/Textures/" + "ProceduralTextureForTest321.png";
            if (File.Exists(fullFilePath))
            {
                File.Delete(fullFilePath); //DELETE previously made test texture file, if existing}
                if (projectTextureCount != 0)
                {
                    projectTextureCount -= 1;
                }
            }

            Texture2D texture;

            texture = new Texture2D(resolution, resolution); //defaults: mipmaps = true & format = automatic
            texture.SetPixel(0, 0, Random.ColorHSV());
            texture.name = "ProceduralTextureForTest321";

            texture.Apply();

            byte[] bytes = texture.EncodeToPNG(); //Graphics Format should be PNG

            File.WriteAllBytes(fullFilePath, bytes);
            AssetDatabase.Refresh();
            projectTextureCount += 1;
            Debug.Log(("Number of Textures in Project: " + projectTextureCount));
        }

        [Test]
        public void Texture_Properties_AreReported()
        {
            var TextureTests = Analyze(IssueCategory.Texture);

            Assert.AreEqual(projectTextureCount, TextureTests.Length, "Checked Texture Count");

            Assert.AreEqual("ProceduralTextureForTest321", TextureTests[0].customProperties[0]);

            Assert.AreEqual("Image", TextureTests[0].customProperties[2]);

            Assert.AreEqual("AutomaticCompressed", TextureTests[0].customProperties[3]);

            Assert.AreEqual("True", TextureTests[0].customProperties[5]);


            Assert.AreEqual("False", TextureTests[0].customProperties[6]);

            Assert.AreEqual((resolution + "x" + resolution).ToString(), TextureTests[0].customProperties[7]);
        }
    }
}
