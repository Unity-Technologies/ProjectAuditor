using System;
using System.Collections.Generic;
using System.IO;
using Unity.ProjectAuditor.Editor.Utils;

namespace Unity.ProjectAuditor.Editor.Core
{
    static class DescriptorLoader
    {
        internal static List<Descriptor> LoadFromJson(string path, string name)
        {
            var rawDescriptors = Json.DeserializeArrayFromFile<Descriptor>(Path.Combine(path, name + ".json"));
            var descriptors = new List<Descriptor>(rawDescriptors.Length);
            foreach (var rawDescriptor in rawDescriptors)
            {
                if (string.IsNullOrEmpty(rawDescriptor.Id))
                    throw new Exception("Descriptor with null id loaded from " + name);

                var desc = new Descriptor(rawDescriptor.Id, rawDescriptor.Title, rawDescriptor.Areas, rawDescriptor.Description, rawDescriptor.Recommendation)
                {
                    Type = rawDescriptor.Type ?? string.Empty,
                    Method = rawDescriptor.Method ?? string.Empty,
                    Value = rawDescriptor.Value,
                    Platforms = rawDescriptor.Platforms,
                    DefaultSeverity = rawDescriptor.DefaultSeverity == Severity.Default ? Severity.Moderate : rawDescriptor.DefaultSeverity,
                    DocumentationUrl = rawDescriptor.DocumentationUrl ?? string.Empty,
                    MinimumVersion = rawDescriptor.MinimumVersion ?? string.Empty,
                    MaximumVersion = rawDescriptor.MaximumVersion ?? string.Empty
                };
                if (string.IsNullOrEmpty(desc.Title))
                {
                    if (string.IsNullOrEmpty(desc.Type) || string.IsNullOrEmpty(desc.Method))
                        desc.Title = string.Empty;
                    else
                        desc.Title = desc.GetFullTypeName();
                }

                descriptors.Add(desc);
            }

            return descriptors;
        }
    }
}
