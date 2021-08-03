using System;
using System.Collections.Generic;
using System.Linq;
using Unity.ProjectAuditor.Editor.UI.Framework;
using Unity.ProjectAuditor.Editor.Auditors;
using UnityEditor;
using UnityEngine;

namespace Unity.ProjectAuditor.Editor.UI
{
    class SummaryView : AnalysisView
    {
        int m_NumBuildSteps;
        int m_NumCodeIssues;
        int m_NumCompilerErrors;
        int m_NumSettingIssues;
        int m_NumResources;
        int m_NumShaders;

        public SummaryView(ViewManager viewManager) : base(viewManager)
        {
        }

        public override void AddIssues(IEnumerable<ProjectIssue> allIssues)
        {
            base.AddIssues(allIssues);

            m_NumBuildSteps += allIssues.Count(i => i.category == IssueCategory.BuildStep);
            m_NumCodeIssues += allIssues.Count(i => i.category == IssueCategory.Code);
            m_NumSettingIssues += allIssues.Count(i => i.category == IssueCategory.ProjectSetting);
            m_NumResources += allIssues.Count(i => i.category == IssueCategory.Asset);
            m_NumShaders += allIssues.Count(i => i.category == IssueCategory.Shader);

            var compilerMessages = allIssues.Where(i => i.category == IssueCategory.CodeCompilerMessage);
            m_NumCompilerErrors += compilerMessages.Count(i => i.severity == Rule.Severity.Error);
        }

        public override void Clear()
        {
            base.Clear();

            m_NumBuildSteps = 0;
            m_NumCodeIssues = 0;
            m_NumCompilerErrors = 0;
            m_NumSettingIssues = 0;
            m_NumResources = 0;
            m_NumShaders = 0;
        }

        protected override void OnDrawInfo()
        {
            EditorGUILayout.LabelField("Analysis overview", EditorStyles.boldLabel);

            EditorGUI.indentLevel++;
            DrawSummaryItem("Code Issues: ", m_NumCodeIssues, IssueCategory.Code);
            if (m_NumCompilerErrors > 0)
            {
                DrawSummaryItem("Compilation Errors: ", m_NumCompilerErrors, IssueCategory.CodeCompilerMessage, Utility.ErrorIcon);
            }
            DrawSummaryItem("Settings Issues:", m_NumSettingIssues, IssueCategory.ProjectSetting);
            DrawSummaryItem("Assets in Resources folders:", m_NumResources, IssueCategory.Asset);
            DrawSummaryItem("Shaders in the project:", m_NumShaders, IssueCategory.Shader);
            var buildAvailable = m_NumBuildSteps > 0;
            DrawSummaryItem("Build Report available:", buildAvailable, IssueCategory.BuildStep);
            EditorGUI.indentLevel--;

            EditorGUILayout.Space();

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
                if (GUILayout.Button(valueAsString, SharedStyles.LinkLabel))
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
