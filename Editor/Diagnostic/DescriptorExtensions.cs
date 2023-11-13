using System;
using System.Linq;
using System.Text.RegularExpressions;
using Unity.ProjectAuditor.Editor.Utils;
using UnityEditor;
using UnityEngine;

namespace Unity.ProjectAuditor.Editor.Diagnostic
{
    internal static class DescriptorExtensions
    {
        public static Area[] GetAreas(this Descriptor descriptor)
        {
            return descriptor.areas.Select(a => (Area)Enum.Parse(typeof(Area), a)).ToArray();
        }

        public static string GetAreasSummary(this Descriptor descriptor)
        {
            return Formatting.CombineStrings(descriptor.areas);
        }

        public static string GetPlatformsSummary(this Descriptor descriptor)
        {
            return (descriptor.platforms == null || descriptor.platforms.Length == 0) ? "Any" : Formatting.CombineStrings(descriptor.platforms);
        }

        public static string GetFullTypeName(this Descriptor descriptor)
        {
            return descriptor.type + "." + descriptor.method;
        }

        /// <summary>
        /// Check if the descriptor applies to the given platform
        /// </summary>
        public static bool IsPlatformCompatible(this Descriptor descriptor, BuildTarget buildTarget)
        {
            if (descriptor.platforms == null || descriptor.platforms.Length == 0)
                return true;
            return descriptor.platforms.Contains(buildTarget.ToString());
        }

        public static bool IsApplicable(this Descriptor desc, AnalysisParams analysisParams)
        {
            return desc.IsVersionCompatible() && desc.IsPlatformCompatible(analysisParams.Platform);
        }

        /// <summary>
        /// Check if any descriptor's platforms are supported by the current editor
        /// </summary>
        public static bool IsPlatformSupported(this Descriptor desc)
        {
            var platforms = desc.platforms;
            if (platforms == null)
                return true;
            foreach (var platform in platforms)
            {
                var buildTarget = (BuildTarget)System.Enum.Parse(typeof(BuildTarget), platform, true);
                var buildTargetGroup = BuildPipeline.GetBuildTargetGroup(buildTarget);
                if (BuildPipeline.IsBuildTargetSupported(buildTargetGroup, buildTarget))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Check if the descriptor applies only to the given platform
        /// </summary>
        public static bool IsPlatformSpecific(this Descriptor descriptor, BuildTarget buildTarget)
        {
            if (descriptor.platforms == null || descriptor.platforms.Length != 1)
                return false;
            return descriptor.platforms[0].Equals(buildTarget.ToString());
        }

        static Version s_UnityVersion = (Version)null;

        /// <summary>
        /// Check if the descriptor's version is compatible with the current editor
        /// </summary>
        public static bool IsVersionCompatible(this Descriptor desc)
        {
            if (s_UnityVersion == null)
            {
                var unityVersionString = Application.unityVersion;
                unityVersionString = unityVersionString.Remove(
                    Regex.Match(unityVersionString, "[A-Za-z]").Index);
                s_UnityVersion = new Version(unityVersionString);
            }

            var minimumVersion = (Version)null;
            var maximumVersion = (Version)null;

            if (!string.IsNullOrEmpty(desc.minimumVersion))
            {
                try
                {
                    minimumVersion = new Version(desc.minimumVersion);
                }
                catch (Exception exception)
                {
                    Debug.LogErrorFormat("Descriptor ({0}) minimumVersion ({1}) is invalid. Exception: {2}", desc.id, desc.minimumVersion, exception.Message);
                }
            }

            if (!string.IsNullOrEmpty(desc.maximumVersion))
            {
                try
                {
                    maximumVersion = new Version(desc.maximumVersion);
                }
                catch (Exception exception)
                {
                    Debug.LogErrorFormat("Descriptor ({0}) maximumVersion ({1}) is invalid. Exception: {2}", desc.id, desc.maximumVersion, exception.Message);
                }
            }

            if (minimumVersion != null && maximumVersion != null && minimumVersion > maximumVersion)
            {
                Debug.LogErrorFormat("Descriptor ({0}) minimumVersion ({1}) is greater than maximumVersion ({2}).", desc.id, minimumVersion, maximumVersion);
                return false;
            }

            if (minimumVersion != null && s_UnityVersion < minimumVersion)
                return false;
            if (maximumVersion != null && s_UnityVersion > maximumVersion)
                return false;

            return true;
        }
    }
}
