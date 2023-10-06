using System;
using System.Collections.Generic;
using System.IO;
using Unity.ProjectAuditor.Editor.Utils;

namespace Unity.ProjectAuditor.Editor.Diagnostic
{
    static class DescriptorLoader
    {
        internal static List<Descriptor> LoadFromJson(string path, string name)
        {
            var rawDescriptors = Json.FromFile<Descriptor>(Path.Combine(path, name + ".json"));
            var descriptors = new List<Descriptor>(rawDescriptors.Length);
            foreach (var rawDescriptor in rawDescriptors)
            {
                var desc = new Descriptor(rawDescriptor.id, rawDescriptor.title, rawDescriptor.areas, rawDescriptor.description, rawDescriptor.solution)
                {
                    type = rawDescriptor.type ?? string.Empty,
                    method = rawDescriptor.method ?? string.Empty,
                    value = rawDescriptor.value,
                    platforms = rawDescriptor.platforms,
                    defaultSeverity = rawDescriptor.defaultSeverity == Severity.Default ? Severity.Moderate : rawDescriptor.defaultSeverity,
                    documentationUrl = rawDescriptor.documentationUrl ?? string.Empty,
                    minimumVersion = rawDescriptor.minimumVersion ?? string.Empty,
                    maximumVersion = rawDescriptor.maximumVersion ?? string.Empty

                };
                if (string.IsNullOrEmpty(desc.title))
                {
                    if (string.IsNullOrEmpty(desc.type) || string.IsNullOrEmpty(desc.method))
                        desc.title = string.Empty;
                    else
                        desc.title = desc.GetFullTypeName();
                }

                descriptors.Add(desc);
            }

            return descriptors;
        }
    }
}
