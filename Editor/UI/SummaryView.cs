using System;
using System.Linq;
using Editor.UI.Framework;
using Unity.ProjectAuditor.Editor.Auditors;
using Unity.ProjectAuditor.Editor.UI.Framework;
using UnityEditor;
using UnityEngine;

namespace Unity.ProjectAuditor.Editor.UI
{
    class SummaryView : AnalysisView
    {
        public SummaryView(ViewManager viewManager) : base(viewManager)
        {
        }

        protected override void OnDrawInfo()
        {
            if (s_Report != null)
            {
                EditorGUILayout.LabelField("Analysis overview", EditorStyles.boldLabel);

                EditorGUI.indentLevel++;
                DrawSummaryItem("Code Issues: ", s_Report.GetIssues(IssueCategory.Code).Length, IssueCategory.Code);
                var numCompilationErrors = s_Report.GetIssues(IssueCategory.CodeCompilerMessage).Count(i => i.severity == Rule.Severity.Error);
                if (numCompilationErrors > 0)
                {
                    DrawSummaryItem("Compilation Errors: ", numCompilationErrors, IssueCategory.CodeCompilerMessage, Utility.ErrorIcon);
                }
                DrawSummaryItem("Settings Issues:", s_Report.GetIssues(IssueCategory.ProjectSetting).Length, IssueCategory.ProjectSetting);
                DrawSummaryItem("Assets in Resources folders:", s_Report.GetIssues(IssueCategory.Asset).Length, IssueCategory.Asset);
                DrawSummaryItem("Shaders in the project:", s_Report.GetIssues(IssueCategory.Shader).Length, IssueCategory.Shader);
                var buildAvailable = s_Report.GetIssues(IssueCategory.BuildFile).Length > 0;
                DrawSummaryItem("Build Report available:", buildAvailable, IssueCategory.BuildStep);
                EditorGUI.indentLevel--;

                EditorGUILayout.Space();
            }

            EditorGUILayout.LabelField("Select a View from the toolbar to start browsing the report");
        }

        public override void DrawContent()
        {
            EditorGUILayout.Space();
            EditorGUILayout.BeginVertical();
            EditorGUI.indentLevel++;

            // note that m_Issues might change during background analysis.
            foreach (var issue in m_Issues.ToArray())
            {
                DrawKeyValue(issue.description, issue.GetCustomProperty(MetaDataProperty.Value));
            }
            EditorGUI.indentLevel--;
            EditorGUILayout.EndVertical();
        }

        void DrawKeyValue(string key, string value)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(string.Format("{0}:", key), GUILayout.ExpandWidth(false));
            EditorGUILayout.LabelField(value,  GUILayout.ExpandWidth(true));
            EditorGUILayout.EndHorizontal();
        }

        void DrawSummaryItem<T>(string title, T value, IssueCategory category, GUIContent icon = null)
        {
            var viewLink = true;
            var valueAsString = value.ToString();
            if (typeof(T) == typeof(bool))
            {
                var valueAsBool = (bool)(object)value;
                valueAsString = valueAsBool ? "Yes" : "No";
                viewLink = valueAsBool;
            }

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(title, GUILayout.ExpandWidth(false));

            if (viewLink)
            {
#if UNITY_2019_2_OR_NEWER
                if (GUILayout.Button(valueAsString, Utility.GetStyle("LinkLabel")))
                    m_ViewManager.ChangeView(category);
#else
                EditorGUILayout.LabelField(valueAsString, GUILayout.MaxWidth(90), GUILayout.ExpandWidth(false));
                if (GUILayout.Button("View", EditorStyles.miniButton, GUILayout.Width(50)))
                    m_ViewManager.ChangeView(category);
#endif
            }
            else
            {
                EditorGUILayout.LabelField(valueAsString, GUILayout.MaxWidth(90), GUILayout.ExpandWidth(false));
            }
            if (icon != null)
                EditorGUILayout.LabelField(icon);
            EditorGUILayout.EndHorizontal();
        }
    }
}
