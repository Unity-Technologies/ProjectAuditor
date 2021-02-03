using System;
using System.Collections.Generic;
using System.IO;
using Unity.ProjectAuditor.Editor.Serialize;
using UnityEditorInternal;

namespace Unity.ProjectAuditor.Editor.Utils
{
    static class ProblemDescriptorLoader
    {
        public static List<ProblemDescriptor> LoadFromJson(string path, string name)
        {
            var fullPath = Path.GetFullPath(Path.Combine(path, name + ".json"));
            var json = File.ReadAllText(fullPath);
            var rawDescriptors = Json.From<ProblemDescriptor>(json);
            var descriptors = new List<ProblemDescriptor>(rawDescriptors.Length);
            var unityVersion = InternalEditorUtility.GetUnityVersion();

            foreach (var rawDescriptor in rawDescriptors)
            {
#if UNITY_2019_1_OR_NEWER
                Version minimumVersion;
                Version maximumVersion;
                if (Version.TryParse(rawDescriptor.minimumVersion, out minimumVersion) && unityVersion < minimumVersion)
                    continue;
                if (Version.TryParse(rawDescriptor.maximumVersion, out maximumVersion) && unityVersion > maximumVersion)
                    continue;
#endif
                var desc = new ProblemDescriptor(rawDescriptor.id, rawDescriptor.description)
                {
                    area = rawDescriptor.area,
                    customevaluator = rawDescriptor.customevaluator,
                    type = rawDescriptor.type,
                    method = rawDescriptor.method,
                    value = rawDescriptor.value,
                    critical = rawDescriptor.critical,
                    problem = rawDescriptor.problem,
                    solution = rawDescriptor.solution
                };
                if (string.IsNullOrEmpty(desc.description))
                {
                    if (string.IsNullOrEmpty(desc.type) || string.IsNullOrEmpty(desc.method))
                        desc.description = string.Empty;
                    else
                        desc.description = desc.type + "." + desc.method;
                }

                descriptors.Add(desc);
            }

            return descriptors;
        }
    }
}
