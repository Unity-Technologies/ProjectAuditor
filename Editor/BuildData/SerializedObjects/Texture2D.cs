using Unity.ProjectAuditor.Editor.UnityFileSystemApi.TypeTreeReaders;

namespace Unity.ProjectAuditor.Editor.BuildData.SerializedObjects
{
    public class Texture2D : SerializedObject
    {
        public enum TextureFormat
        {
            None = 0,

            Alpha8 = 1,           // In memory: A8U
            ARGB4444 = 2,         // In memory: A4U,R4U,G4U,B4U; 0xBGRA if viewed as 16 bit word on little-endian, equivalent to VK_FORMAT_R4G4B4A4_UNORM_PACK16, Pixel layout depends on endianness, A4;R4;G4;B4 going from high to low bits.
            RGB24 = 3,            // In memory: R8U,G8U,B8U
            RGBA32 = 4,           // In memory: R8U,G8U,B8U,A8U; 0xAABBGGRR if viewed as 32 bit word on little-endian. Generally preferred for 32 bit uncompressed data.
            ARGB32 = 5,           // In memory: A8U,R8U,G8U,B8U; 0xBBGGRRAA if viewed as 32 bit word on little-endian
            ARGBFloat = 6,        // only for internal use at runtime
            RGB565 = 7,           // In memory: R5U,G6U,B5U; 0xBGR if viewed as 16 bit word on little-endian, equivalent to VK_FORMAT_R5G6B5_UNORM_PACK16, Pixel layout depends on endianness, R5;G6;B5 going from high to low bits
            BGR24 = 8,            // In memory: B8U,G8U,R8U
            R16 = 9,              // In memory: R16U

            // DXT/S3TC compression
            DXT1 = 10,            // aka BC1
            DXT3 = 11,            // aka BC2
            DXT5 = 12,            // aka BC3

            RGBA4444 = 13,        // In memory: A4U,R4U,G4U,B4U; 0xARGB if viewed as 16 bit word on little-endian, Pixel layout depends on endianness, R4;G4;B4;A4 going from high to low bits

            BGRA32    = 14,       // In memory: B8U,G8U,R8U,A8U; 0xAARRGGBB if viewed as 32 bit word on little-endian. Used by some WebCam implementations.

            // float/half texture formats
            RHalf = 15,           // In memory: R16F
            RGHalf = 16,          // In memory: R16F,G16F
            RGBAHalf = 17,        // In memory: R16F,G16F,B16F,A16F
            RFloat = 18,          // In memory: R32F
            RGFloat = 19,         // In memory: R32F,G32F
            RGBAFloat = 20,       // In memory: R32F,G32F,B32F,A32F

            YUY2 = 21,            // YUV format, can be used for video streams.

            // Three partial-precision floating-point numbers encoded into a single 32-bit value all sharing the same
            // 5-bit exponent (variant of s10e5, which is sign bit, 10-bit mantissa, and 5-bit biased(15) exponent).
            // There is no sign bit, and there is a shared 5-bit biased(15) exponent and a 9-bit mantissa for each channel.
            RGB9e5Float = 22,

            RGBFloat  = 23,       // Editor only format (used for saving HDR)

            // DX10/DX11 (aka BPTC/RGTC) compressed formats
            BC6H  = 24,           // RGB HDR compressed format, unsigned.
            BC7   = 25,           // HQ RGB(A) compressed format.
            BC4   = 26,           // One-component compressed format, 0..1 range.
            BC5   = 27,           // Two-component compressed format, 0..1 range.

            // Crunch compression
            DXT1Crunched = 28,    // DXT1 Crunched
            DXT5Crunched = 29,    // DXT5 Crunched

            // PowerVR / iOS PVRTC compression
            PVRTC_RGB2 = 30,
            PVRTC_RGBA2 = 31,
            PVRTC_RGB4 = 32,
            PVRTC_RGBA4 = 33,

            // OpenGL ES 2.0 ETC
            ETC_RGB4 = 34,

            // EAC and ETC2 compressed formats, in OpenGL ES 3.0
            EAC_R = 41,
            EAC_R_SIGNED = 42,
            EAC_RG = 43,
            EAC_RG_SIGNED = 44,
            ETC2_RGB = 45,
            ETC2_RGBA1 = 46,
            ETC2_RGBA8 = 47,

            // ASTC. The RGB and RGBA formats are internally identical.
            // before we had ASTC_RGB_NxN and ASTC_RGBA_NxN, thats why we have hole here
            ASTC_4x4 = 48,
            ASTC_5x5 = 49,
            ASTC_6x6 = 50,
            ASTC_8x8 = 51,
            ASTC_10x10 = 52,
            ASTC_12x12 = 53,
            // [54..59] were taken by ASTC_RGBA_NxN

            // Nintendo 3DS
            ETC_RGB4_3DS = 60,
            ETC_RGBA8_3DS = 61,

            RG16 = 62,
            R8 = 63,

            // Crunch compression for ETC format
            ETC_RGB4Crunched = 64,
            ETC2_RGBA8Crunched = 65,

            ASTC_HDR_4x4 = 66,
            ASTC_HDR_5x5 = 67,
            ASTC_HDR_6x6 = 68,
            ASTC_HDR_8x8 = 69,
            ASTC_HDR_10x10 = 70,
            ASTC_HDR_12x12 = 71,

            // 16-bit raw integer formats
            RG32 = 72,
            RGB48 = 73,
            RGBA64 = 74,

            R8_SIGNED = 75,
            RG16_SIGNED = 76,
            RGB24_SIGNED = 77,
            RGBA32_SIGNED = 78,

            R16_SIGNED = 79,
            RG32_SIGNED = 80,
            RGB48_SIGNED = 81,
            RGBA64_SIGNED = 82,
        };

        public int Width { get; }
        public int Height { get; }
        public TextureFormat Format { get; }
        public int MipCount { get; }
        public bool RwEnabled { get; }

        public Texture2D(RandomAccessReader reader, long size, BuildFileInfo buildFile)
            : base(reader, size, "Texture2D", buildFile)
        {
            Width = reader["m_Width"].GetValue<int>();
            Height = reader["m_Height"].GetValue<int>();
            Format = (TextureFormat)reader["m_TextureFormat"].GetValue<int>();
            RwEnabled = reader["m_IsReadable"].GetValue<int>() != 0;
            MipCount = reader["m_MipCount"].GetValue<int>();

            Size += reader["image data"].GetArraySize() == 0 ? reader["m_StreamData"]["size"].GetValue<int>() : 0;
        }
    }
}
