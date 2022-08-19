using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using Unity.ProjectAuditor.Editor;
using System.IO;
using Unity.ProjectAuditor.Editor.Modules;
using Unity.ProjectAuditor.Editor.Utils;
using Unity.ProjectAuditor.EditorTests;
using UnityEditor;
using UnityEngine;

namespace unity.ProjectAuditor.EditorTests
{
    class DeleteThisTestScript : TestFixtureBase


//public class DeleteThisTestScript : MonoBehaviour
    {
        public static string fullFilePath = Application.dataPath + "/Textures/" + "RandomPNGImageForTest321" + ".png";

        public static Texture2D texture;

        public static int resolution = 1;

        [MenuItem("Quick Tests/Texture Access")]
        private static void HelloWorld()
        {
            runStuff();
        }

        // Start is called before the first frame update
        static public void runStuff()
        {
            if (File.Exists(fullFilePath)) {  File.Delete(fullFilePath);  Debug.Log("Just deleted file: " + fullFilePath); } //DELETE previously made test texture file, if existing}

            Debug.Log("Starting to run the Setup");
            texture = new Texture2D(resolution, resolution, TextureFormat.RGB24, false);
            texture.SetPixel(0, 0, Random.ColorHSV());
            texture.name = "Procedural Texture";
            texture.Apply();

            byte[] bytes = texture.EncodeToPNG();

            File.WriteAllBytes(fullFilePath, bytes);
            AssetDatabase.Refresh();
            RunMoreStuff();
        }

        // Update is called once per frame
        static public void RunMoreStuff()
        {
            // string testerstring = "/Textures/" + "RandomPNGImageForTest321" + ".png";

            // var tName = ((Texture2D)AssetDatabase.LoadAssetAtPath(testerstring, typeof(Texture2D)));
            // var fullpathoftexture =
            //      AssetDatabase.GetAssetPath(Application.dataPath + "/Textures/" + "RandomPNGImageForTest321" + ".png"); //Texture's location (path) which includes the texture filename
            var textureImporter = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(texture)) as TextureImporter;

            Debug.Log("Printing value, and need it not to be blank: " + textureImporter);
        }
    }
}
