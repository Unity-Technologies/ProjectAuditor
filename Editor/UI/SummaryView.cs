using System;
using UnityEditor;
using UnityEngine;

namespace Unity.ProjectAuditor.Editor.UI
{
    public class SummaryView : AnalysisView
    {
        public static Action<IssueCategory> OnChangeView;

        protected override void OnDrawInfo()
        {
            if (s_Report != null)
            {
                EditorGUILayout.LabelField("Analysis overview", EditorStyles.boldLabel);

                EditorGUI.indentLevel++;
                SummaryItem("Code Issues: " + s_Report.GetIssues(IssueCategory.Code).Length, IssueCategory.Code);
                SummaryItem("Settings Issues: " + s_Report.GetIssues(IssueCategory.ProjectSettings).Length, IssueCategory.ProjectSettings);
                SummaryItem("Assets in Resources folders: " + s_Report.GetIssues(IssueCategory.Assets).Length, IssueCategory.Assets);
                SummaryItem("Shaders in the project: " + s_Report.GetIssues(IssueCategory.Shaders).Length, IssueCategory.Shaders);
                EditorGUI.indentLevel--;

                EditorGUILayout.Space();
            }

            EditorGUILayout.LabelField("Select a View from the toolbar to start browsing the report");
        }

        static void SummaryItem(string text, IssueCategory category)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(text);
            if (GUILayout.Button("View", EditorStyles.miniButton))
                OnChangeView(category);
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }
    }
}
