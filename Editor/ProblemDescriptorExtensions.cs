using System;
using System.Linq;
using Unity.ProjectAuditor.Editor.Utils;
using UnityEditor;

namespace Unity.ProjectAuditor.Editor
{
    public static class ProblemDescriptorExtensions
    {
        public static Area[] GetAreas(this ProblemDescriptor descriptor)
        {
            return descriptor.areas.Select(a => (Area)Enum.Parse(typeof(Area), a)).ToArray();
        }

        public static string GetAreasSummary(this ProblemDescriptor descriptor)
        {
            return Formatting.CombineStrings(descriptor.areas);
        }

        public static string GetFullTypeName(this ProblemDescriptor descriptor)
        {
            return descriptor.type + "." + descriptor.method;
        }

        public static bool IsPlatformCompatible(this ProblemDescriptor descriptor, BuildTarget buildTarget)
        {
            if (descriptor.platforms == null)
                return true;
            return descriptor.platforms.Contains(buildTarget.ToString());
        }
    }
}
