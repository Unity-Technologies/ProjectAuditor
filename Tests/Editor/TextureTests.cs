using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using Unity.ProjectAuditor.Editor;
using System.IO;
using Unity.ProjectAuditor.Editor.Modules;
using Unity.ProjectAuditor.Editor.Utils;
using Unity.ProjectAuditor.EditorTests;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEditor.VersionControl;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.TestTools;

namespace unity.ProjectAuditor.EditorTests
{
    class TextureTests : TestFixtureBase
    {
        private Texture2D texture;
        private int resolution = 1;
        public string fullFilePath = Application.dataPath + "/Textures/" + "RandomPNGImageForTest321" + ".png";


        [OneTimeSetUp]
        public void SetUp()
        {
            if (File.Exists(fullFilePath)) {File.Delete(fullFilePath);}      //DELETE previously made test texture file, if existing}

            texture = new Texture2D(resolution, resolution, TextureFormat.RGB24, false);
            texture.SetPixel(0, 0, Random.ColorHSV());
            //   texture.name = "Procedural Texture";
            texture.Apply();

            byte[] bytes = texture.EncodeToPNG();

            File.WriteAllBytes(fullFilePath, bytes);

            AssetDatabase.Refresh();
        }

        [Test]
        public void Texture_Properties_AreReported()
        {
            // First, ensure there's a text texture already created/existing.
            Assert.IsTrue(texture != null);

            //   var pathToTexture = AssetDatabase.GUIDToAssetPath(texture);
            //Prepare and Load Platform Specific Texture Import (Property) Settings for comparison with the test texture's properties
            var textureImporter = (TextureImporter)AssetImporter.GetAtPath(fullFilePath);               //Need this for comparing current texture's import platform settings
            var platformDefaultTextureImportFormat = textureImporter.GetDefaultPlatformTextureSettings().format.ToString();                 //Grab the default texture import format for this platform
            var defaultPlatformTextureSettings =  textureImporter.GetDefaultPlatformTextureSettings();            //Actually grab the (few) specific values in platformtexturesettings (Android) for comparison


            // Compare default platform texture import (IsReadable) with Test Texture's (IsReadable) property
            Assert.IsTrue(textureImporter.isReadable == texture.isReadable);


            // Compare default texture import format with test texture format
            // If the platform uses "automatic" to select format, display exactly what format will be chosen by  "Automatic"
            if (platformDefaultTextureImportFormat == "AutomaticCompressed") { platformDefaultTextureImportFormat = textureImporter.GetAutomaticFormat("Android").ToString(); }

            Assert.IsTrue(platformDefaultTextureImportFormat.ToString() == texture.format.ToString());


            // Compare default texture import compression with Test Texture's compression
            Assert.IsTrue(defaultPlatformTextureSettings.textureCompression.ToString() == textureImporter.GetPlatformTextureSettings("Android").textureCompression.ToString());


            // Compare default texture shape(dimension) with Test Texture property
            Assert.IsTrue(textureImporter.textureShape.ToString() == texture.dimension.ToString());


            Debug.Log("Gettin' to the end of the Texture Tests in the code!");
            // }
        }
    }
}
// var currentTestTexture = (Texture2D)AssetDatabase.LoadAssetAtPath(path, typeof(Texture2D)); // Grab the actual Texture for properties comparison, not just the string (name)
