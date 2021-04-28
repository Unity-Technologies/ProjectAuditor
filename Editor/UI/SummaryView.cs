using System;
using System.Linq;
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
                SummaryItem("Code Issues: ", s_Report.GetIssues(IssueCategory.Code).Length, IssueCategory.Code);
                var numCompilationErrors = s_Report.GetIssues(IssueCategory.CodeCompilerMessages).Count(i => i.severity == Rule.Severity.Error);
                if (numCompilationErrors > 0)
                {
                    SummaryItem("Compilation Errors: ", numCompilationErrors, IssueCategory.CodeCompilerMessages, Utility.ErrorIcon);
                }
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

        static void SummaryItem(string title, int value, IssueCategory category, GUIContent icon = null)
        {
            SummaryItem(title, value.ToString(), category, icon);
        }

        static void SummaryItem(string title, string value, IssueCategory category, GUIContent icon = null)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(title, GUILayout.ExpandWidth(false));
            EditorGUILayout.LabelField(value, GUILayout.MaxWidth(90), GUILayout.ExpandWidth(false));
            if (GUILayout.Button("View", EditorStyles.miniButton, GUILayout.Width(50)))
                OnChangeView(category);
            if (icon != null)
                EditorGUILayout.LabelField(icon);
            EditorGUILayout.EndHorizontal();
        }
    }
}
