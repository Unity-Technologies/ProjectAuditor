using System;
using System.Linq;
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

                var compilerMessages = s_Report.GetIssues(IssueCategory.CodeCompilerMessages);
                var numCompilationErrors = compilerMessages.Count(i => i.severity == Rule.Severity.Error);
                if (numCompilationErrors > 0)
                {
                    SummaryItem("Compilation Errors: ", s_Report.GetIssues(IssueCategory.CodeCompilerMessages).Length, IssueCategory.CodeCompilerMessages, EditorGUIUtility.TrIconContent(Utility.ErrorIconName));
                }
                else
                {
                    SummaryItem("Code Issues: ", s_Report.GetIssues(IssueCategory.Code).Length, IssueCategory.Code);
                }
                SummaryItem("Settings Issues:", s_Report.GetIssues(IssueCategory.ProjectSettings).Length, IssueCategory.ProjectSettings);
                SummaryItem("Assets in Resources folders:", s_Report.GetIssues(IssueCategory.Assets).Length, IssueCategory.Assets);
                SummaryItem("Shaders in the project:", s_Report.GetIssues(IssueCategory.Shaders).Length, IssueCategory.Shaders);
                var buildAvailable = s_Report.GetIssues(IssueCategory.BuildFiles).Length > 0;
                SummaryItem("Build Report available:", buildAvailable.ToString(), IssueCategory.BuildFiles);
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
            EditorGUILayout.LabelField(title);
            EditorGUILayout.LabelField(value, GUILayout.Width(60));
            if (GUILayout.Button("View", EditorStyles.miniButton))
                OnChangeView(category);
            if (icon != null)
                EditorGUILayout.LabelField(icon);
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }
    }
}
