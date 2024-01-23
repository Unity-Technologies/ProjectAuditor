using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Unity.ProjectAuditor.Editor.Core
{
    internal static class CoreUtils
    {
        public static bool SupportsPlatform(Type type, BuildTarget platform)
        {
            if (!type.CustomAttributes.Any())
                return true;
            return type.GetCustomAttributes<AnalysisPlatformAttribute>().Any(a => a.Platform == platform);
        }

        public static Severity LogTypeToSeverity(LogType logType)
        {
            switch (logType)
            {
                case LogType.Assert:
                case LogType.Error:
                case LogType.Exception:
                    return Severity.Error;
                case LogType.Warning:
                    return Severity.Warning;
                default:
                    return Severity.Info;
            }
        }
    }
}
