using System.Linq;
using Unity.ProjectAuditor.Editor.Utils;
using UnityEditor;
using UnityEditor.PackageManager;

namespace Unity.ProjectAuditor.Editor
{
    internal class ProjectAuditorPackage
    {
        const string k_CanonicalPath = "Packages/" + Name;

        static ProjectAuditorPackage()
        {
            var paths = AssetDatabase.FindAssets("t:asmdef", new string[] { "Packages" })
                .Select(AssetDatabase.GUIDToAssetPath);
            var asmDefPath = paths.FirstOrDefault(path => path.EndsWith("Unity.ProjectAuditor.Editor.asmdef"));
            Path = string.IsNullOrEmpty(asmDefPath) ?
                k_CanonicalPath :
                PathUtils.GetDirectoryName(PathUtils.GetDirectoryName(asmDefPath));

            var packageInfo = PackageUtils.GetClientPackages().First(p => p.name == Name);

            IsLocal = packageInfo.source == PackageSource.Local;
            Version = packageInfo.version;
        }

        public static bool IsLocal { get; }

        public const string Name = "com.unity.project-auditor";

        public static string Path { get; }

        public static string Version { get; }
    }
}
