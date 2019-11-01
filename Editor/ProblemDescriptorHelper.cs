using System.Collections.Generic;
using System.IO;

namespace Unity.ProjectAuditor.Editor
{
    public static class ProblemDescriptorHelper
    {
        public static List<ProblemDescriptor> LoadProblemDescriptors(string path, string name)
        {
            var descriptors = new List<ProblemDescriptor>();

            var fullPath = Path.GetFullPath(Path.Combine(path, name + ".json"));
            var json = File.ReadAllText(fullPath);
            var result = JsonHelper.FromJson<ProblemDescriptor>(json);

            descriptors.AddRange(result);

            return descriptors;
        }
    }
}