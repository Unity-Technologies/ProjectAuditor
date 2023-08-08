using UnityEngine;

namespace Unity.ProjectAuditor.Editor
{
    /// <summary>
    /// Project-specific user settings to adjust values/limits analyzers test against
    /// </summary>
    [CreateAssetMenu(menuName = "Project Auditor/Project Auditor Settings")]
    public class ProjectAuditorSettings : ScriptableObject
    {
        /// <summary>
        /// Maximum mesh vertex count. If we find meshes with more triangles we report an issue.
        /// </summary>
        public int MeshVerticeCountLimit = 5000;

        /// <summary>
        /// Maximum mesh vertex count. If we find meshes with more triangles we report an issue.
        /// </summary>
        public int MeshTriangleCountLimit = 5000;

        /// <summary>
        /// Maximum texture size. If we find textures at this size we warn the user. If they exceed the size we report a critical issue.
        /// </summary>
        public int TextureSizeLimit = 2048;

        /// <summary>
        /// Maximum size of all files in the StreamingAssets folder. Beyond that size we report an issue.
        /// </summary>
        public int StreamingAssetsFolderSizeLimit = 50;

        /// <summary>
        /// Maximum size when it is not necessary to enable Streaming Mipmaps. Beyond that size we report an issue.
        /// </summary>
        public int TextureStreamingMipmapsSizeLimit = 4000;

        /// <summary>
        /// Percent of empty space allowed in a Sprite Atlas texture. Beyond that size we report an issue.
        /// </summary>
        public int SpriteAtlasEmptySpaceLimit = 50;

        /// <summary>
        /// The runtime size (in bytes) above which we report an issue for non-streaming AudioClips.
        /// </summary>

        /// <description>
        /// The default value is the memory footprint of a single currently-playing instance of a streaming stereo 48KHz AudioClip.
        /// </description>
        public int StreamingClipThresholdBytes = 1 * (64000 + (int)(1.6 * 48000 * 2)) + 694;

        /// <summary>
        /// The runtime size (in bytes) above which we report an issue for AudioClips that are set to Decompress On Load.
        /// </summary>
        public int LongDecompressedClipThresholdBytes = 200 * 1024;

        /// <summary>
        /// The file size (in bytes) above which we report advice on reducing the size of compressed AudioClips on mobile.
        /// </summary>
        public int LongCompressedMobileClipThresholdBytes = 200 * 1024;

        /// <summary>
        /// The file size (in bytes) above which we recommend that AudioClips be flagged to load in the background.
        /// </summary>
        public int LoadInBackGroundClipSizeThresholdBytes = 200 * 1024;
    }
}
