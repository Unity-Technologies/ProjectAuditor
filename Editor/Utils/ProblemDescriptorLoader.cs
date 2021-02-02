using System;
using System.Collections.Generic;
using System.IO;
using Unity.ProjectAuditor.Editor.Serialize;

namespace Unity.ProjectAuditor.Editor.Utils
{
    static class ProblemDescriptorLoader
    {
        public static List<ProblemDescriptor> LoadFromJson(string path, string name)
        {
            var fullPath = Path.GetFullPath(Path.Combine(path, name + ".json"));
            var json = File.ReadAllText(fullPath);
            var descriptors = Json.From<ProblemDescriptor>(json);

            foreach (var desc in descriptors)
            {
                if (string.IsNullOrEmpty(desc.description))
                {
                    if (string.IsNullOrEmpty(desc.type) || string.IsNullOrEmpty(desc.method))
                        desc.description = string.Empty;
                    else
                        desc.description = desc.type + "." + desc.method;
                }
            }

            return new List<ProblemDescriptor>(descriptors);
        }
    }
}
