using System;
using System.Linq;

namespace Unity.ProjectAuditor.Editor.Utils
{
    public static class ProblemDescriptorExtensions
    {
        public static Area[] GetAreas(this ProblemDescriptor descriptor)
        {
            return descriptor.areas.Select(a => (Area)Enum.Parse(typeof(Area), a)).ToArray();
        }

        public static string GetAreasSummary(this ProblemDescriptor descriptor)
        {
            return string.Join(", ", descriptor.areas);
        }
    }
}
