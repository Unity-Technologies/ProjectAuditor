using System;
using System.Reflection;
using UnityEditor;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.U2D;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace Unity.ProjectAuditor.Editor.Utils
{
    internal static class TextureUtils
    {
        /// <summary>
        /// Check if a texture is a single solid color and its size is over than 1x1
        /// </summary>
        /// <param name="textureImporter">The texture importer of the texture.</param>
        /// <param name="texture">The texture to check.</param>
        /// <returns>True if the texture is a single solid color above 1x1.</returns>
        public static bool IsTextureSolidColorTooBig(TextureImporter textureImporter, Texture texture)
        {
            bool isTooBig = false;

            if (texture == null)
            {
                Debug.LogWarning($"Could not load texture at {textureImporter.assetPath}");
                return false;
            }

            // Skip non-2D textures (which don't support GetPixels)
            if (!(texture is Texture2D texture2D))
                return false;

            // Skip textures which are child assets (fonts, embedded textures, etc.)
            if (!AssetDatabase.IsMainAsset(texture))
                return false;

            if (texture.width == 1 && texture.height == 1)
            {
                return false;
            }

            // For non-readable textures, make it readable to use some functions (GetPixels())
            if (textureImporter.isReadable)
            {
                isTooBig = IsSolidColor(texture2D);
            }
            else
            {
                Texture2D copyTexture = CopyTexture(texture2D);
                isTooBig = IsSolidColor(copyTexture);
                //Release texture from Memory
                Object.DestroyImmediate(copyTexture);
            }

            return isTooBig;
        }

        /// <summary>
        /// Check if a texture is comprised of a single solid color.
        /// </summary>
        /// <param name="texture">The texture to check.</param>
        /// <returns>True if the texture is a single solid color.</returns>
        static bool IsSolidColor(Texture2D texture)
        {
            // Skip "degenerate" textures like font atlases
            if (texture.width == 0 || texture.height == 0)
            {
                return false;
            }

            //Optimization lines
            //As GetPixels function can be costly, run a first test to check if texture is not solid color
            var pixel1 = texture.GetPixel(0, 0);
            var pixel2 = texture.width != 0 ? texture.GetPixel(1, 0) : texture.GetPixel(0, 1);

            if (pixel1 != pixel2)
            {
                return false;
            }

            var pixels = texture.GetPixels32();

            // It is unlikely to get a null pixels array, but we should check just in case
            if (pixels == null)
            {
                Debug.LogWarning($"Could not read {texture}");
                return false;
            }

            // It is unlikely, but possible that we got this far and there are no pixels.
            var pixelCount = pixels.Length;
            if (pixelCount == 0)
            {
                Debug.LogWarning($"No pixels in {texture}");
                return false;
            }

            // Convert to int for faster comparison
            var colorValue = Color32ToInt.Convert(pixels[0]);
            var isSolidColor = true;
            for (var i = 1; i < pixelCount; i++)
            {
                var pixel = Color32ToInt.Convert(pixels[i]);
                if (pixel != colorValue)
                {
                    isSolidColor = false;
                    break;
                }
            }

            return isSolidColor;
        }

        /// <summary>
        /// Get the percent of empty space not used in a sprite atlas
        /// </summary>
        /// <param name="spriteAtlas">The Sprite Atlas to check.</param>
        /// <returns>The percent of empty space.</returns>
        public static int GetEmptySpacePercentage(SpriteAtlas spriteAtlas)
        {
            var method = typeof(SpriteAtlasExtensions).GetMethod("GetPreviewTextures", BindingFlags.Static | BindingFlags.NonPublic);
            object obj = method.Invoke(null, new object[] { spriteAtlas });
            Texture2D[] textures = obj as Texture2D[];

            if (textures == null || textures.Length == 0)
            {
                Debug.LogError($"Could not load texture from {spriteAtlas.name}");
                return 0;
            }
            //Get the main texture of the Sprite Atlas
            Texture2D texture = textures[0];

            if (texture == null)
            {
                Debug.LogError("Texture of the " + spriteAtlas.name + "Sprite Atlas was not found.");
                return 0;
            }

            var emptyPercent = GetEmptyPixelsPercent(texture);
            return emptyPercent;
        }

        public static int GetEmptyPixelsPercent(Texture2D texture2D)
        {
            Color32[] pixels;

            if (texture2D.width == 0 || texture2D.height == 0)
            {
                return 0;
            }

            if (texture2D.isReadable)
            {
                pixels = texture2D.GetPixels32();
            }
            else
            {
                var copyTexture = CopyTexture(texture2D);
                if (copyTexture == null)
                {
                    Debug.LogWarning($"Could not copy {texture2D}");
                    return 0;
                }

                pixels = copyTexture.GetPixels32();
                //Release texture from Memory
                Object.DestroyImmediate(copyTexture);
            }

            // It is unlikely to get a null pixels array, but we should check just in case
            if (pixels == null)
            {
                Debug.LogWarning($"Could not read {texture2D}");
                return 0;
            }

            // It is unlikely, but possible that we got this far and there are no pixels.
            var pixelCount = pixels.Length;
            if (pixelCount == 0)
            {
                Debug.LogWarning($"No pixels in {texture2D}");
                return 0;
            }

            int transparencyPixelsCount = 0;

            for (var i = 1; i < pixelCount; i++)
            {
                if (pixels[i].a == 0)
                {
                    transparencyPixelsCount++;
                }
            }

            double percent = (double)transparencyPixelsCount / (double)pixelCount;
            return ((int)Math.Round(percent * 100));
        }

        static Texture2D CopyTexture(Texture2D texture)
        {
            RenderTexture tmp = RenderTexture.GetTemporary(
                texture.width,
                texture.height,
                0,
                RenderTextureFormat.ARGB32,
                RenderTextureReadWrite.Default);


            // Backup the currently set RenderTexture
            RenderTexture previous = RenderTexture.active;

            Graphics.Blit(texture, tmp);
            RenderTexture.active = tmp;

            Texture2D newTexture = new Texture2D(texture.width, texture.height);
            newTexture.ReadPixels(new Rect(0, 0, tmp.width, tmp.height), 0, 0);
            newTexture.Apply();

            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(tmp);

            return newTexture;
        }
    }
}
