using System;
using System.Linq;
using Unity.ProjectAuditor.Editor.Utils;
using UnityEditor;

namespace Unity.ProjectAuditor.Editor.Diagnostic
{
    internal static class DescriptorExtensions
    {
        internal static Area[] GetAreas(this Descriptor descriptor)
        {
            return descriptor.areas.Select(a => (Area)Enum.Parse(typeof(Area), a)).ToArray();
        }

        internal static string GetAreasSummary(this Descriptor descriptor)
        {
            return Formatting.CombineStrings(descriptor.areas);
        }

        internal static string GetPlatformsSummary(this Descriptor descriptor)
        {
            return (descriptor.platforms == null || descriptor.platforms.Length == 0) ? "Any" : Formatting.CombineStrings(descriptor.platforms);
        }

        internal static string GetFullTypeName(this Descriptor descriptor)
        {
            return descriptor.type + "." + descriptor.method;
        }

        /// <summary>
        /// Check if the descriptor applies to the given platform
        /// </summary>
        internal static bool IsPlatformCompatible(this Descriptor descriptor, BuildTarget buildTarget)
        {
            if (descriptor.platforms == null || descriptor.platforms.Length == 0)
                return true;
            return descriptor.platforms.Contains(buildTarget.ToString());
        }

        /// <summary>
        /// Check if the descriptor applies only to the given platform
        /// </summary>
        internal static bool IsPlatformSpecific(this Descriptor descriptor, BuildTarget buildTarget)
        {
            if (descriptor.platforms == null || descriptor.platforms.Length != 1)
                return false;
            return descriptor.platforms[0].Equals(buildTarget.ToString());
        }
    }
}
