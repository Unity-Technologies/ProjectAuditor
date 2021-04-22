using System;
using UnityEditor;
using UnityEngine;

namespace Unity.ProjectAuditor.Editor.UI
{
    class SummaryView : AnalysisView
    {
        protected override void OnDrawInfo()
        {
            if (s_Report != null)
            {
                EditorGUILayout.LabelField("Analysis overview", EditorStyles.boldLabel);

                EditorGUI.indentLevel++;
                SummaryItem("Code Issues:", s_Report.GetIssues(IssueCategory.Code).Length, IssueCategory.Code);
                SummaryItem("Settings Issues:", s_Report.GetIssues(IssueCategory.ProjectSettings).Length, IssueCategory.ProjectSettings);
                SummaryItem("Assets in Resources folders:", s_Report.GetIssues(IssueCategory.Assets).Length, IssueCategory.Assets);
                SummaryItem("Shaders in the project:", s_Report.GetIssues(IssueCategory.Shaders).Length, IssueCategory.Shaders);
                var buildAvailable = s_Report.GetIssues(IssueCategory.BuildFiles).Length > 0;
                SummaryItem("Build Report available:", buildAvailable ? "Yes" : "No", IssueCategory.BuildFiles);
                EditorGUI.indentLevel--;

                EditorGUILayout.Space();
            }

            EditorGUILayout.LabelField("Select a View from the toolbar to start browsing the report");
        }

        static void SummaryItem(string title, int value, IssueCategory category)
        {
            SummaryItem(title, value.ToString(), category);
        }

        static void SummaryItem(string title, string value, IssueCategory category)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(title, GUILayout.ExpandWidth(false));
            EditorGUILayout.LabelField(value, GUILayout.MaxWidth(90), GUILayout.ExpandWidth(false));
            if (GUILayout.Button("View", EditorStyles.miniButton, GUILayout.Width(50)))
                OnChangeView(category);
            EditorGUILayout.EndHorizontal();
        }
    }
}
