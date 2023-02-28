using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Unity.ProjectAuditor.Editor.Modules
{
    /// <summary>
    /// Scans textures ans check if they are single solid color ones.
    /// </summary>
    /// <author>
    /// Unity Labs SuperScience
    /// <author>
    public static class ScanSolidTexture
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
            bool originalValue = textureImporter.isReadable;

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

            // For non-readable textures, make it readable to use some functions (GetPixels())
            if (!textureImporter.isReadable)
            {
                textureImporter.isReadable = true;
                textureImporter.SaveAndReimport();
            }

            if (IsSolidColor(texture2D))
            {
                if (texture.width != 1 && texture.height != 1)
                {
                    isTooBig = true;
                }
            }

            textureImporter.isReadable = originalValue;
            textureImporter.SaveAndReimport();

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

            var pixels = texture.GetPixels();

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
    }

    /// <summary>
    /// Conversion struct which takes advantage of Color32 struct layout for fast conversion to and from Int32.
    /// </summary>
    /// <author>
    /// Unity Labs SuperScience
    /// <author>
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
