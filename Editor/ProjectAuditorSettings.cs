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
        internal int MeshVerticeCountLimit = 5000;

        /// <summary>
        /// Maximum mesh vertex count. If we find meshes with more triangles we report an issue.
        /// </summary>
        internal int MeshTriangleCountLimit = 5000;

        /// <summary>
        /// Maximum texture size. If we find textures at this size we warn the user. If they exceed the size we report a critical issue.
        /// </summary>
        internal int TextureSizeLimit = 2048;

        /// <summary>
        /// Maximum size of all files in the StreamingAssets folder. Beyond that size we report an issue.
        /// </summary>
        internal int StreamingAssetsFolderSizeLimit = 50;

        /// <summary>
        /// Maximum size when it is not necessary to enable Streaming Mipmaps. Beyond that size we report an issue.
        /// </summary>
        internal int TextureStreamingMipmapsSizeLimit = 4000;

        /// <summary>
        /// Percent of empty space allowed in a Sprite Atlas texture. Beyond that size we report an issue.
        /// </summary>
        internal int SpriteAtlasEmptySpaceLimit = 50;

        /// <summary>
        /// The runtime size (in bytes) above which we report an issue for non-streaming AudioClips. Unity's AudioClip streaming buffers are 200KB in size.
        /// </summary>
        internal int StreamingClipThresholdBytes = 200 * 1024;

        /// <summary>
        /// The runtime size (in bytes) above which we report an issue for AudioClips that are set to Decompress On Load.
        /// </summary>
        internal int LongDecompressedClipThresholdBytes = 200 * 1024;

        /// <summary>
        /// The file size (in bytes) above which we report advice on reducing the size of compressed AudioClips on mobile.
        /// </summary>
        internal int LongCompressedMobileClipThresholdBytes = 200 * 1024;

        /// <summary>
        /// The file size (in bytes) above which we recommend that AudioClips be flagged to load in the background.
        /// </summary>
        internal int LoadInBackGroundClipSizeThresholdBytes = 200 * 1024;
    }
}
