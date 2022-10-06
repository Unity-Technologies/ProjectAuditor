using System;
using System.Linq;
using Unity.ProjectAuditor.Editor.Utils;
using UnityEditor;

namespace Unity.ProjectAuditor.Editor.Diagnostic
{
    public static class DescriptorExtensions
    {
        public static Area[] GetAreas(this Descriptor descriptor)
        {
            return descriptor.areas.Select(a => (Area)Enum.Parse(typeof(Area), a)).ToArray();
        }

        public static string GetAreasSummary(this Descriptor descriptor)
        {
            return Formatting.CombineStrings(descriptor.areas);
        }

        public static string GetFullTypeName(this Descriptor descriptor)
        {
            return descriptor.type + "." + descriptor.method;
        }

        public static bool IsPlatformCompatible(this Descriptor descriptor, BuildTarget buildTarget)
        {
            if (descriptor.platforms == null)
                return true;
            return descriptor.platforms.Contains(buildTarget.ToString());
        }
    }
}
