using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Unity.ProjectAuditor.Editor.Modules
{
    /// <summary>
    /// Scans textures ans check if they are single solid color ones.
    /// </summary>
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
            TextureFormat format = texture.format;

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

    /// <summary>
    /// Conversion struct which takes advantage of Color32 struct layout for fast conversion to and from Int32.
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    public struct Color32ToInt
    {
        /// <summary>
        /// Int field which shares an offset with the color field.
        /// Set m_Color to read a converted value from this field.
        /// </summary>
        [FieldOffset(0)] int m_Int;

        /// <summary>
        /// Color32 field which shares an offset with the int field.
        /// Set m_Int to read a converted value from this field.
        /// </summary>
        [FieldOffset(0)] Color32 m_Color;

        /// <summary>
        /// The int value.
        /// </summary>
        public int Int => m_Int;

        /// <summary>
        /// The color value.
        /// </summary>
        public Color32 Color => m_Color;

        /// <summary>
        /// Constructor for Color32 to Int32 conversion.
        /// </summary>
        /// <param name="color">The color which will be converted to an int.</param>
        Color32ToInt(Color32 color)
        {
            m_Int = 0;
            m_Color = color;
        }

        /// <summary>
        /// Constructor for Int32 to Color32 conversion.
        /// </summary>
        /// <param name="value">The int which will be converted to an Color32.</param>
        Color32ToInt(int value)
        {
            m_Color = default;
            m_Int = value;
        }

        /// <summary>
        /// Convert a Color32 to an Int32.
        /// </summary>
        /// <param name="color">The Color32 which will be converted to an int.</param>
        /// <returns>The int value for the given color.</returns>
        public static int Convert(Color32 color)
        {
            var convert = new Color32ToInt(color);
            return convert.m_Int;
        }

        /// <summary>
        /// Convert a Color32 to an Int32.
        /// </summary>
        /// <param name="value">The int which will be converted to an Color32.</param>
        /// <returns>The Color32 value for the given int.</returns>
        public static Color32 Convert(int value)
        {
            var convert = new Color32ToInt(value);
            return convert.m_Color;
        }
    }
}
