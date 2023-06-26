using System;
using System.IO;
using System.Linq;
using Unity.ProjectAuditor.Editor.Core;
using Unity.ProjectAuditor.Editor.Diagnostic;
using UnityEditor;
using UnityEngine;

namespace Unity.ProjectAuditor.Editor.UI.Framework
{
    internal class DiagnosticView : AnalysisView
    {
        public override string description => $"A list of {m_Desc.displayName} issues found in the project.";

        public DiagnosticView(ViewManager viewManager) : base(viewManager)
        {
        }

        public override void DrawDetails(ProjectIssue[] selectedIssues)
        {
            var selectedDescriptors = selectedIssues.Select(i => i.descriptor).Distinct().ToArray();

            EditorGUILayout.BeginVertical(GUILayout.Width(LayoutSize.FoldoutWidth));

            EditorGUILayout.LabelField(Contents.Details, SharedStyles.BoldLabel);
            {
                if (selectedDescriptors.Length == 0)
                    GUILayout.TextArea(k_NoSelectionText, SharedStyles.TextAreaWithDynamicSize, GUILayout.MaxHeight(LayoutSize.FoldoutMaxHeight));
                else if (selectedDescriptors.Length > 1)
                    GUILayout.TextArea(k_MultipleSelectionText, SharedStyles.TextAreaWithDynamicSize, GUILayout.MaxHeight(LayoutSize.FoldoutMaxHeight));
                else // if (selectedDescriptors.Length == 1)
                    GUILayout.TextArea(selectedDescriptors[0].description, SharedStyles.TextAreaWithDynamicSize, GUILayout.MaxHeight(LayoutSize.FoldoutMaxHeight));
            }

            EditorGUILayout.LabelField(Contents.Recommendation, SharedStyles.BoldLabel);
            {
                if (selectedDescriptors.Length == 0)
                    GUILayout.TextArea(k_NoSelectionText, SharedStyles.TextAreaWithDynamicSize, GUILayout.MaxHeight(LayoutSize.FoldoutMaxHeight));
                else if (selectedDescriptors.Length > 1)
                    GUILayout.TextArea(k_MultipleSelectionText, SharedStyles.TextAreaWithDynamicSize, GUILayout.MaxHeight(LayoutSize.FoldoutMaxHeight));
                else // if (selectedDescriptors.Length == 1)
                    GUILayout.TextArea(selectedDescriptors[0].solution, SharedStyles.TextAreaWithDynamicSize, GUILayout.MaxHeight(LayoutSize.FoldoutMaxHeight));
            }

            if (selectedDescriptors.Length == 1)
            {
                if (!string.IsNullOrEmpty(selectedDescriptors[0].documentationUrl))
                {
                    DrawActionButton(Contents.Documentation, () =>
                    {
                        Application.OpenURL(selectedDescriptors[0].documentationUrl);
                    });
                }

                if (selectedDescriptors[0].fixer != null)
                {
                    GUI.enabled = selectedIssues.Any(i => !i.wasFixed);

                    DrawActionButton(Contents.QuickFix, () =>
                    {
                        foreach (var issue in selectedIssues)
                        {
                            selectedDescriptors[0].Fix(issue);
                        }
                    });

                    GUI.enabled = true;
                }
            }

            EditorGUILayout.EndVertical();
        }

        protected override void DrawInfo()
        {
            EditorGUILayout.LabelField("\u2022 Use the Filters to reduce the number of reported issues");
            EditorGUILayout.LabelField("\u2022 Use the Mute button to mark an issue as false-positive");
        }

        protected override void Export(Func<ProjectIssue, bool> predicate = null)
        {
            var path = EditorUtility.SaveFilePanel("Save to CSV file", UserPreferences.loadSavePath, string.Format("project-auditor-{0}.csv", m_Desc.category.ToString()).ToLower(),
                "csv");
            if (path.Length != 0)
            {
                using (var exporter = new CSVExporter(path, m_Layout))
                {
                    exporter.WriteHeader();

                    var matchingIssues = m_Issues.Where(issue => predicate == null || predicate(issue));
                    matchingIssues = matchingIssues.Where(issue => issue.descriptor.IsValid() || m_Config.GetAction(issue.descriptor, issue.GetContext()) != Severity.None);
                    exporter.WriteIssues(matchingIssues.ToArray());
                }

                EditorUtility.RevealInFinder(path);

                if (m_ViewManager.onViewExported != null)
                    m_ViewManager.onViewExported();

                UserPreferences.loadSavePath = Path.GetDirectoryName(path);
            }
        }

        static class Contents
        {
            public static readonly GUIContent Details = new GUIContent("Details:", "Issue Details");
            public static readonly GUIContent Recommendation =
                new GUIContent("Recommendation:", "Recommendation on how to solve the issue");
            public static readonly GUIContent Documentation = new GUIContent("Documentation");
            public static readonly GUIContent QuickFix = new GUIContent("Quick Fix");
        }
    }
}
