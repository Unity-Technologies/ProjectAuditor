namespace Unity.ProjectAuditor.Editor.Utils
{
    public static class PackageUtils
    {
        public static string GetPackageRecommendedVersion(UnityEditor.PackageManager.PackageInfo package)
        {
#if UNITY_2019_1_OR_NEWER
            return package.versions.verified;
#else
            return package.versions.recommended;
#endif
        }
    }
}
