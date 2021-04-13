using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Unity.ProjectAuditor.Editor.UI
{
    public class SummaryView : AnalysisView
    {
        static ProjectReport m_Report;

        public static Action<IssueCategory> OnChangeView;

        public static void SetReport(ProjectReport report)
        {
            m_Report = report;
        }

        protected override void OnDrawInfo()
        {
            if (m_Report != null)
            {
                EditorGUILayout.LabelField("Analysis overview", EditorStyles.boldLabel);

                EditorGUILayout.BeginHorizontal();
                var numCompilationErrors = m_Report
                    .GetIssues(IssueCategory.CodeCompilerMessages).Count(i => i.severity == Rule.Severity.Error);
                if (numCompilationErrors > 0)
                {
                    EditorGUILayout.LabelField("Compilation Errors: " + numCompilationErrors);
                    if (GUILayout.Button("View", EditorStyles.miniButton))
                        OnChangeView(IssueCategory.CodeCompilerMessages);
                }
                else
                {
                    EditorGUILayout.LabelField("Code Issues: " + m_Report.GetIssues(IssueCategory.Code).Length);
                    if (GUILayout.Button("View", EditorStyles.miniButton))
                        OnChangeView(IssueCategory.Code);
                }
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Settings Issues: " + m_Report.GetIssues(IssueCategory.ProjectSettings).Length);
                if (GUILayout.Button("View", EditorStyles.miniButton))
                    OnChangeView(IssueCategory.ProjectSettings);
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Assets in Resources folders: " + m_Report.GetIssues(IssueCategory.Assets).Length);
                if (GUILayout.Button("View", EditorStyles.miniButton))
                    OnChangeView(IssueCategory.Assets);
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Shaders in the project: " + m_Report.GetIssues(IssueCategory.Shaders).Length);
                if (GUILayout.Button("View", EditorStyles.miniButton))
                    OnChangeView(IssueCategory.Shaders);
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.Space();
            }

            EditorGUILayout.LabelField("Select a View from the toolbar to start browsing the report");
        }
    }
}
