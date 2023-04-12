using System;
using System.Linq;
using System.Reflection;
using UnityEditor;

namespace Unity.ProjectAuditor.Editor.Core
{
    internal static class CoreUtils
    {
        internal static bool SupportsPlatform(Type type, BuildTarget platform)
        {
            if (!type.CustomAttributes.Any())
                return true;
            return type.GetCustomAttributes<AnalysisPlatformAttribute>().Any(a => a.platform == platform);
        }
    }
}
