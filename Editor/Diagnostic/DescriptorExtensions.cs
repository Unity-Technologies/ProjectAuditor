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

        /// <summary>
        /// Check if the descriptor applies only to the given platform
        /// </summary>
        public static bool IsPlatformSpecific(this Descriptor descriptor, BuildTarget buildTarget)
        {
            if (descriptor.platforms == null || descriptor.platforms.Length != 1)
                return false;
            return descriptor.platforms[0].Equals(buildTarget.ToString());
        }

        public static Descriptor WithDocumentationUrl(this Descriptor descriptor, string documentationUrl)
        {
            descriptor.documentationUrl = documentationUrl;
            return descriptor;
        }

        public static Descriptor WithFixer(this Descriptor descriptor, Action<ProjectIssue> fixer)
        {
            descriptor.fixer = fixer;
            return descriptor;
        }

        public static Descriptor WithMessageFormat(this Descriptor descriptor, string messageFormat)
        {
            descriptor.messageFormat = messageFormat;
            return descriptor;
        }

        public static Descriptor WithPlatforms(this Descriptor descriptor, BuildTarget[] platforms)
        {
            descriptor.platforms = platforms.Select(p => p.ToString()).ToArray();
            return descriptor;
        }

        public static Descriptor WithSeverity(this Descriptor descriptor, Severity severity)
        {
            descriptor.severity = severity;
            return descriptor;
        }
    }
}
