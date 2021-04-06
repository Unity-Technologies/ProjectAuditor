using Packages.Editor.Utils;
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
            if (report == null)
            {
                EditorGUILayout.LabelField("Build Report not found");
            }
            else
            {
                EditorGUILayout.LabelField("Build Name: ", Application.productName);
                EditorGUILayout.LabelField("Platform: ", report.summary.platform.ToString());
                EditorGUILayout.LabelField("Started at: ", report.summary.buildStartedAt.ToString());
                EditorGUILayout.LabelField("Ended at: ", report.summary.buildEndedAt.ToString());
                EditorGUILayout.LabelField("Total Time: ", Formatting.FormatTime(report.summary.totalTime));
                EditorGUILayout.LabelField("Total Size: ", Formatting.FormatSize(report.summary.totalSize));
                EditorGUILayout.LabelField("Build Result: ", report.summary.result.ToString());
            }
        }
    }
}
