using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEditor.PackageManager;
using Unity.ProjectAuditor.Editor;
using UnityEngine;

namespace Unity.ProjectAuditor.Editor.Utils
{
    internal static class PackageUtils
    {
        const string k_UnknownVersion = "Unknown";

        public static int CompareVersions(string lhs, string rhs)
        {
            const string regex = "[^0-9.]";
            var leftStr = Regex.Replace(lhs, regex, "", RegexOptions.IgnoreCase);
            var rightStr = Regex.Replace(rhs, regex, "", RegexOptions.IgnoreCase);
            var leftVersion = new Version(leftStr);
            var rightVersion = new Version(rightStr);
            return leftVersion.CompareTo(rightVersion);
        }

        public static PackageInfo[] GetClientPackages()
        {
#if UNITY_2021_1_OR_NEWER
            return PackageInfo.GetAllRegisteredPackages();
#elif UNITY_2019_1_OR_NEWER
            var getAllMethod = typeof(PackageInfo).GetMethod("GetAll", BindingFlags.Static | BindingFlags.NonPublic);
            if (getAllMethod != null)
            {
                return getAllMethod.Invoke(null, new object[] {}) as PackageInfo[];
            }
#else
            var type = Type.GetType("UnityEditor.PackageManager.Packages, UnityEditor");
            if (type != null)
            {
                var getAllMethod = type.GetMethod("GetAll", BindingFlags.Static | BindingFlags.Public);
                if (getAllMethod != null)
                {
                    return getAllMethod.Invoke(null, new object[] {}) as PackageInfo[];
                }
            }
#endif
            throw new NotSupportedException("PackageInfo.GetAll() is not available.");
        }

        public static string GetClientPackageVersion(string packageName)
        {
            var packages = GetClientPackages();
            if (packages != null)
            {
                foreach (var packageInfo in packages)
                {
                    if (packageInfo.name == packageName)
                        return packageInfo.version;
                }
            }

            Debug.LogWarning($"Can't find Package {packageName}.");

            return k_UnknownVersion;
        }

        public static string GetPackageRecommendedVersion(UnityEditor.PackageManager.PackageInfo package)
        {
#if UNITY_2022_2_OR_NEWER
            return package.versions.recommended;
#elif UNITY_2019_1_OR_NEWER
            return package.versions.verified;
#else
            return package.versions.recommended;
#endif
        }

        public static bool IsClientPackage(string packageName)
        {
            var packages = GetClientPackages();
            if (packages != null)
            {
                foreach (var packageInfo in packages)
                {
                    if (packageInfo.name == packageName)
                        return true;
                }
            }


            Debug.LogWarning($"Can't find Package {packageName}.");

            return false;
        }
    }
}
