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
                if (string.IsNullOrEmpty(rawDescriptor.description))
                {
                    if (string.IsNullOrEmpty(rawDescriptor.type) || string.IsNullOrEmpty(rawDescriptor.method))
                        rawDescriptor.description = string.Empty;
                    else
                        rawDescriptor.description = rawDescriptor.type + "." + rawDescriptor.method;
                }

                descriptors.Add(desc);
            }

            return descriptors;
        }
    }
}
