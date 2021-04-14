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

                EditorGUILayout.BeginHorizontal();
                var compilerMessages = s_Report.GetIssues(IssueCategory.CodeCompilerMessages);
                var numCompilationErrors = compilerMessages.Count(i => i.severity == Rule.Severity.Error);
                if (numCompilationErrors > 0)
                {
                    EditorGUILayout.LabelField("Compilation Errors: " + numCompilationErrors);
                    if (GUILayout.Button("View", EditorStyles.miniButton))
                        OnChangeView(IssueCategory.CodeCompilerMessages);
                    EditorGUILayout.LabelField(EditorGUIUtility.TrIconContent(Utility.ErrorIconName));
                }
                else
                {
                    EditorGUILayout.LabelField("Code Issues: " + s_Report.GetIssues(IssueCategory.Code).Length);
                    if (GUILayout.Button("View", EditorStyles.miniButton))
                        OnChangeView(IssueCategory.Code);
                }
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Settings Issues: " + s_Report.GetIssues(IssueCategory.ProjectSettings).Length);
                if (GUILayout.Button("View", EditorStyles.miniButton))
                    OnChangeView(IssueCategory.ProjectSettings);
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Assets in Resources folders: " + s_Report.GetIssues(IssueCategory.Assets).Length);
                if (GUILayout.Button("View", EditorStyles.miniButton))
                    OnChangeView(IssueCategory.Assets);
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Shaders in the project: " + s_Report.GetIssues(IssueCategory.Shaders).Length);
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
