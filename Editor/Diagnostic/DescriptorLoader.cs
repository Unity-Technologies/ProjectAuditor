using System;
using System.Collections.Generic;
using System.IO;
using Unity.ProjectAuditor.Editor.Utils;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Unity.ProjectAuditor.Editor.Diagnostic
{
    static class DescriptorLoader
    {
        public static List<Descriptor> LoadFromJson(string path, string name)
        {
            var rawDescriptors = Json.FromFile<Descriptor>(Path.Combine(path, name + ".json"));
            var descriptors = new List<Descriptor>(rawDescriptors.Length);
            foreach (var rawDescriptor in rawDescriptors)
            {
                if (!IsPlatformCompatible(rawDescriptor))
                    continue;

                if (!IsVersionCompatible(rawDescriptor))
                    continue;

                var desc = new Descriptor(rawDescriptor.id, rawDescriptor.title, rawDescriptor.areas)
                {
                    customevaluator = rawDescriptor.customevaluator,
                    type = rawDescriptor.type ?? string.Empty,
                    method = rawDescriptor.method ?? string.Empty,
                    value = rawDescriptor.value,
                    platforms = rawDescriptor.platforms,
                    critical = rawDescriptor.critical,
                    description = rawDescriptor.description,
                    solution = rawDescriptor.solution
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

        internal static bool IsPlatformCompatible(Descriptor desc)
        {
            var platforms = desc.platforms;
            if (platforms == null)
                return true;
            foreach (var platform in platforms)
            {
                var buildTarget = (BuildTarget)System.Enum.Parse(typeof(BuildTarget), platform, true);
                var buildTargetGroup = BuildPipeline.GetBuildTargetGroup(buildTarget);
                if (BuildPipeline.IsBuildTargetSupported(buildTargetGroup, buildTarget))
                    return true;
            }

            return false;
        }

        internal static bool IsVersionCompatible(Descriptor desc)
        {
            var unityVersion = InternalEditorUtility.GetUnityVersion();
            var minimumVersion = (Version)null;
            var maximumVersion = (Version)null;

            if (!string.IsNullOrEmpty(desc.minimumVersion))
            {
                try
                {
                    minimumVersion = new Version(desc.minimumVersion);
                }
                catch (Exception exception)
                {
                    Debug.LogErrorFormat("Descriptor ({0}) minimumVersion ({1}) is invalid. Exception: {2}", desc.id, desc.minimumVersion, exception.Message);
                }
            }

            if (!string.IsNullOrEmpty(desc.maximumVersion))
            {
                try
                {
                    maximumVersion = new Version(desc.maximumVersion);
                }
                catch (Exception exception)
                {
                    Debug.LogErrorFormat("Descriptor ({0}) maximumVersion ({1}) is invalid. Exception: {2}", desc.id, desc.maximumVersion, exception.Message);
                }
            }

            if (minimumVersion != null && maximumVersion != null && minimumVersion > maximumVersion)
            {
                Debug.LogErrorFormat("Descriptor ({0}) minimumVersion ({1}) is greater than maximumVersion ({2}).", desc.id, minimumVersion, maximumVersion);
                return false;
            }

            if (minimumVersion != null && unityVersion < minimumVersion)
                return false;
            if (maximumVersion != null && unityVersion > maximumVersion)
                return false;

            return true;
        }
    }
}
