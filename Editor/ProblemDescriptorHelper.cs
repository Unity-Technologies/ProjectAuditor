using System.Collections.Generic;
using System.IO;
using Unity.ProjectAuditor.Editor.Utils;

namespace Unity.ProjectAuditor.Editor
{
    public static class ProblemDescriptorHelper
    {
        public static List<ProblemDescriptor> LoadProblemDescriptors(string path, string name)
        {
            var fullPath = Path.GetFullPath(Path.Combine(path, name + ".json"));
            var json = File.ReadAllText(fullPath);
            var descriptors = JsonHelper.FromJson<ProblemDescriptor>(json);

            foreach (var desc in descriptors)
            {
                if (string.IsNullOrEmpty(desc.type) || string.IsNullOrEmpty(desc.method))
                    desc.description = string.Empty;
                else
                    desc.description = desc.type + "." + desc.method;
            }
            
            return new List<ProblemDescriptor>(descriptors);
        }
    }
}