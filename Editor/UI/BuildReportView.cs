using Unity.ProjectAuditor.Editor.Auditors;
using UnityEditor;
using UnityEngine;

namespace Unity.ProjectAuditor.Editor.UI
{
    class BuildReportView : AnalysisView
    {
        public override void DrawInfo()
        {
            var report = BuildAuditor.GetBuildReport();
            EditorGUILayout.LabelField("Build Name: ", Application.productName);
            EditorGUILayout.LabelField("Platform: ", report.summary.platform.ToString());
            EditorGUILayout.LabelField("Started at: ", report.summary.buildStartedAt.ToString());
            EditorGUILayout.LabelField("Ended at: ", report.summary.buildEndedAt.ToString());
            EditorGUILayout.LabelField("Total Time: ", FormatTime(report.summary.totalTime));
#if UNITY_2019_3_OR_NEWER
            EditorGUILayout.LabelField("Total Size: ", FormatSize(report.summary.totalSize));
            EditorGUILayout.LabelField("Build Result: ", report.summary.result.ToString());
#else
            EditorGUILayout.LabelField("Total Size: ", FormatSize(report.summary.totalSize));
            EditorGUILayout.LabelField("Build Result: ", report.summary.result.ToString());
#endif
        }

        static string FormatTime(System.TimeSpan t)
        {
            return t.Hours + ":" + t.Minutes.ToString("D2") + ":" + t.Seconds.ToString("D2");
        }

        private static string FormatSize(ulong size)
        {
            if (size < 1024)
                return size + " B";
            if (size < 1024 * 1024)
                return (size / 1024.00).ToString("F2") + " KB";
            if (size < 1024 * 1024 * 1024)
                return (size / (1024.0 * 1024.0)).ToString("F2") + " MB";
            return (size / (1024.0 * 1024.0 * 1024.0)).ToString("F2") + " GB";
        }
    }
}
