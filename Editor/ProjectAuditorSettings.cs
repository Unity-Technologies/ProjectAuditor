using UnityEngine;

namespace Unity.ProjectAuditor.Editor
{
    /// <summary>
    /// Project and platform-specific user settings to adjust values/limits analyzers test against
    /// </summary>
    public class ProjectAuditorSettings : ScriptableObject
    {
        /// <summary>
        /// Maximum mesh vertex count. If we find meshes with more triangles we report an issue.
        /// </summary>
        public int MeshVerticeCountThreshold = 5000;

        /// <summary>
        /// Maximum mesh vertex count. If we find meshes with more triangles we report an issue.
        /// </summary>
        public int MeshTriangleCountThreshold = 5000;

        /// <summary>
        /// Maximum texture size. If we find textures at this size we warn the user. If they exceed the size we report a critical issue.
        /// </summary>
        public int TextureSizeThreshold = 2048;

        /// <summary>
        /// Maximum size of all files in the StreamingAssets folder. Beyond that size we report an issue.
        /// </summary>
        public int StreamingAssetsFolderSizeLimitMb = 50;
    }
}
