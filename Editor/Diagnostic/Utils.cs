using UnityEngine;

namespace Unity.ProjectAuditor.Editor.Diagnostic
{
    internal static class Utils
    {
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
