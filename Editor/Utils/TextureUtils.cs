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
            if (texture == null)
            {
                Debug.LogWarning($"Could not load texture at {textureImporter.assetPath}");
                return false;
            }

            // Skip textures of unsupported dimensions
            if (!(
                texture.dimension == UnityEngine.Rendering.TextureDimension.Tex2D
#if PA_CAN_USE_TEXTURE_MIPMAPCOUNT
                || texture.dimension == UnityEngine.Rendering.TextureDimension.Tex2DArray
                || texture.dimension == UnityEngine.Rendering.TextureDimension.Tex3D
                || texture.dimension == UnityEngine.Rendering.TextureDimension.Cube
#endif
                ))
                return false;

            // Skip textures which are child assets (fonts, embedded textures, etc.)
            if (!AssetDatabase.IsMainAsset(texture))
                return false;

            if (texture.width == 1 && texture.height == 1)
            {
                return false;
            }

            return IsSolidColorWithDimensionHandling(textureImporter, texture);
        }

        static bool IsSolidColorWithDimensionHandling(TextureImporter textureImporter, Texture texture)
        {
            bool isTooBig = false;

            // For non-readable textures, make it readable to use some functions (GetPixels())
            switch (texture.dimension)
            {
                case UnityEngine.Rendering.TextureDimension.Tex2D:
                {
                    Texture2D texture2D = texture as Texture2D;

                    if (textureImporter.isReadable)
                    {
                        isTooBig = IsSolidColor(texture2D);
                    }
                    else
                    {
                        Texture2D copyTexture = CopyTexture(texture2D);
                        isTooBig = IsSolidColor(copyTexture);
                        Object.DestroyImmediate(copyTexture);
                    }

                    break;
                }

#if PA_CAN_USE_TEXTURE_MIPMAPCOUNT
                case UnityEngine.Rendering.TextureDimension.Tex2DArray:
                {
                    Texture2DArray texture2DArray = texture as Texture2DArray;

                    if (textureImporter.isReadable)
                    {
                        isTooBig = IsSolidColor(texture2DArray);
                    }
                    else
                    {
                        Texture2DArray copyTexture = CopyTexture(texture2DArray);
                        isTooBig = IsSolidColor(copyTexture);
                        Object.DestroyImmediate(copyTexture);
                    }

                    break;
                }

                case UnityEngine.Rendering.TextureDimension.Tex3D:
                {
                    Texture3D texture3D = texture as Texture3D;

                    if (textureImporter.isReadable)
                    {
                        isTooBig = IsSolidColor(texture3D);
                    }
                    else
                    {
                        Texture3D copyTexture = CopyTexture(texture3D);
                        isTooBig = IsSolidColor(copyTexture);
                        Object.DestroyImmediate(copyTexture);
                    }

                    break;
                }

                case UnityEngine.Rendering.TextureDimension.Cube:
                {
                    Cubemap textureCube = texture as Cubemap;

                    if (textureImporter.isReadable)
                    {
                        isTooBig = IsSolidColor(textureCube);
                    }
                    else
                    {
                        Cubemap copyTexture = CopyTexture(textureCube);
                        isTooBig = IsSolidColor(copyTexture);
                        Object.DestroyImmediate(copyTexture);
                    }

                    break;
                }
#endif
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
            var pixel2 = texture.width > 0 ? texture.GetPixel(1, 0) : texture.GetPixel(0, 1);

            if (pixel1 != pixel2)
            {
                return false;
            }

            Color32[] pixels = null;
            try
            {
                pixels = texture.GetPixels32();
            }
            catch (ArgumentException)
            {
                // in some cases, GetPixels32 fails with a "Texture X has no data." error and throws an exception
                return false;
            }

            // It is unlikely to get a null pixels array, but we should check just in case
            if (pixels == null)
            {
                Debug.LogWarning($"Could not read {texture.name}");
                return false;
            }

            // It is unlikely, but possible that we got this far and there are no pixels.
            var pixelCount = pixels.Length;
            if (pixelCount == 0)
            {
                Debug.LogWarning($"No pixels in {texture.name}");
                return false;
            }

            // Convert to int for faster comparison
            var colorValue = Color32ToInt.Convert(pixels[0]);
            for (var i = 1; i < pixelCount; i++)
            {
                var pixel = Color32ToInt.Convert(pixels[i]);
                if (pixel != colorValue)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Check if each slice in a texture array is comprised of a single solid color.
        /// </summary>
        /// <param name="texture">The texture array to check.</param>
        /// <returns>True if each slice of the texture array is a single solid color.</returns>
        static bool IsSolidColor(Texture2DArray texture)
        {
            // Skip "degenerate" textures like font atlases
            if (texture.width == 0 || texture.height == 0)
            {
                return false;
            }

            // It doesn't matter if all slices are the same solid color, just that they are all solid colors.
            for (int j = 0; j < texture.depth; ++j)
            {
                var pixels = texture.GetPixels32(j);

                // It is unlikely to get a null pixels array, but we should check just in case
                if (pixels == null)
                {
                    Debug.LogWarning($"Could not read {texture.name}");
                    return false;
                }

                // It is unlikely, but possible that we got this far and there are no pixels.
                var pixelCount = pixels.Length;
                if (pixelCount == 0)
                {
                    Debug.LogWarning($"No pixels in {texture.name}");
                    return false;
                }

                // Convert to int for faster comparison
                var colorValue = Color32ToInt.Convert(pixels[0]);
                for (var i = 1; i < pixelCount; i++)
                {
                    var pixel = Color32ToInt.Convert(pixels[i]);
                    if (pixel != colorValue)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// Check if each slice in a 3D texture is comprised of a single solid color.
        /// </summary>
        /// <param name="texture">The 3D texture to check.</param>
        /// <returns>True if each slice of the 3D texture is a single solid color.</returns>
        static bool IsSolidColor(Texture3D texture)
        {
            // Skip "degenerate" textures like font atlases
            if (texture.width == 0 || texture.height == 0)
            {
                return false;
            }

            // It doesn't matter if all slices are the same solid color, just that they are all solid colors.
            for (int j = 0; j < texture.depth; ++j)
            {
                var pixels = texture.GetPixels32(j);

                // It is unlikely to get a null pixels array, but we should check just in case
                if (pixels == null)
                {
                    Debug.LogWarning($"Could not read {texture.name}");
                    return false;
                }

                // It is unlikely, but possible that we got this far and there are no pixels.
                var pixelCount = pixels.Length;
                if (pixelCount == 0)
                {
                    Debug.LogWarning($"No pixels in {texture.name}");
                    return false;
                }

                // Convert to int for faster comparison
                var colorValue = Color32ToInt.Convert(pixels[0]);
                for (var i = 1; i < pixelCount; i++)
                {
                    var pixel = Color32ToInt.Convert(pixels[i]);
                    if (pixel != colorValue)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// Check if each face in a cubemap is comprised of a single solid color.
        /// </summary>
        /// <param name="texture">The cubemap to check.</param>
        /// <returns>True if each face of a cubemap is a single solid color.</returns>
        static bool IsSolidColor(Cubemap texture)
        {
            // Skip "degenerate" textures like font atlases
            if (texture.width == 0 || texture.height == 0)
            {
                return false;
            }

            // It doesn't matter if all faces are the same solid color, just that they are all solid colors.
            for (int j = 0; j < 6; ++j)
            {
                var pixels = texture.GetPixels((CubemapFace)j);

                // It is unlikely to get a null pixels array, but we should check just in case
                if (pixels == null)
                {
                    Debug.LogWarning($"Could not read {texture.name}");
                    return false;
                }

                // It is unlikely, but possible that we got this far and there are no pixels.
                var pixelCount = pixels.Length;
                if (pixelCount == 0)
                {
                    Debug.LogWarning($"No pixels in {texture.name}");
                    return false;
                }

                var colorValue = pixels[0];
                for (var i = 1; i < pixelCount; i++)
                {
                    if (pixels[i] != colorValue)
                    {
                        return false;
                    }
                }
            }

            return true;
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
                    Debug.LogWarning($"Could not copy {texture2D.name}");
                    return 0;
                }

                try
                {
                    pixels = copyTexture.GetPixels32();
                }
                catch (ArgumentException)
                {
                    // in some cases, GetPixels32 fails with a "Texture X has no data." error and throws an exception

                    //Release texture from Memory
                    Object.DestroyImmediate(copyTexture);

                    return 0;
                }

                //Release texture from Memory
                Object.DestroyImmediate(copyTexture);
            }

            // It is unlikely to get a null pixels array, but we should check just in case
            if (pixels == null)
            {
                Debug.LogWarning($"Could not read {texture2D.name}");
                return 0;
            }

            // It is unlikely, but possible that we got this far and there are no pixels.
            var pixelCount = pixels.Length;
            if (pixelCount == 0)
            {
                Debug.LogWarning($"No pixels in {texture2D.name}");
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
#if PA_CAN_USE_TEXTURE_MIPMAPCOUNT
            Texture2D newTexture = new Texture2D(texture.width, texture.height, texture.format, texture.mipmapCount != 1);
            Graphics.CopyTexture(texture, newTexture);
#else
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
#endif
            newTexture.name = texture.name + " (temp)";
            return newTexture;
        }

#if PA_CAN_USE_TEXTURE_MIPMAPCOUNT
        static Texture2DArray CopyTexture(Texture2DArray texture)
        {
            Texture2DArray newTexture = new Texture2DArray(texture.width, texture.height, texture.depth, texture.format, texture.mipmapCount != 1);
            Graphics.CopyTexture(texture, newTexture);

            return newTexture;
        }

        static Texture3D CopyTexture(Texture3D texture)
        {
            Texture3D newTexture = new Texture3D(texture.width, texture.height, texture.depth, texture.format, texture.mipmapCount != 1);
            Graphics.CopyTexture(texture, newTexture);

            return newTexture;
        }

        static Cubemap CopyTexture(Cubemap texture)
        {
            Cubemap newTexture = new Cubemap(texture.width, texture.format, texture.mipmapCount != 1);
            Graphics.CopyTexture(texture, newTexture);

            return newTexture;
        }

#endif

        public static int GetTextureDepth(Texture texture)
        {
            int textureDepth = 1;

            switch (texture.dimension)
            {
                case UnityEngine.Rendering.TextureDimension.Tex3D:
                {
                    Texture3D texture3D = texture as Texture3D;
                    textureDepth = texture3D.depth;

                    break;
                }

                case UnityEngine.Rendering.TextureDimension.Cube:
                {
                    textureDepth = 6;

                    break;
                }

                case UnityEngine.Rendering.TextureDimension.Tex2DArray:
                {
                    Texture2DArray texture2DArray = texture as Texture2DArray;
                    textureDepth = texture2DArray.depth;

                    break;
                }

                case UnityEngine.Rendering.TextureDimension.CubeArray:
                {
                    CubemapArray textureCubeArray = texture as CubemapArray;
                    textureDepth = textureCubeArray.cubemapCount * 6;

                    break;
                }
            }

            return textureDepth;
        }
    }
}
