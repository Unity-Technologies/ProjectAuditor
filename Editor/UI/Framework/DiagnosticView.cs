using System;
using System.IO;
using System.Linq;
using Unity.ProjectAuditor.Editor.Core;
using Unity.ProjectAuditor.Editor.Diagnostic;
using UnityEditor;
using UnityEngine;

namespace Unity.ProjectAuditor.Editor.UI.Framework
{
    public class DiagnosticView : AnalysisView
    {
        public DiagnosticView(ViewManager viewManager) : base(viewManager)
        {
        }

        public override void DrawDetails(ProjectIssue[] selectedIssues)
        {
            var selectedDescriptors = selectedIssues.Select(i => i.descriptor).Distinct().ToArray();

            EditorGUILayout.BeginVertical(GUILayout.Width(LayoutSize.FoldoutWidth));

            EditorGUILayout.LabelField(Contents.Details, EditorStyles.boldLabel);
            {
                if (selectedDescriptors.Length == 0)
                    GUILayout.TextArea(k_NoSelectionText, SharedStyles.TextArea, GUILayout.MaxHeight(LayoutSize.FoldoutMaxHeight));
                else if (selectedDescriptors.Length > 1)
                    GUILayout.TextArea(k_MultipleSelectionText, SharedStyles.TextArea, GUILayout.MaxHeight(LayoutSize.FoldoutMaxHeight));
                else // if (selectedDescriptors.Length == 1)
                    GUILayout.TextArea(selectedDescriptors[0].description, SharedStyles.TextArea, GUILayout.MaxHeight(LayoutSize.FoldoutMaxHeight));
            }

            EditorGUILayout.LabelField(Contents.Recommendation, EditorStyles.boldLabel);
            {
                if (selectedDescriptors.Length == 0)
                    GUILayout.TextArea(k_NoSelectionText, SharedStyles.TextArea, GUILayout.MaxHeight(LayoutSize.FoldoutMaxHeight));
                else if (selectedDescriptors.Length > 1)
                    GUILayout.TextArea(k_MultipleSelectionText, SharedStyles.TextArea, GUILayout.MaxHeight(LayoutSize.FoldoutMaxHeight));
                else // if (selectedDescriptors.Length == 1)
                    GUILayout.TextArea(selectedDescriptors[0].solution, SharedStyles.TextArea, GUILayout.MaxHeight(LayoutSize.FoldoutMaxHeight));
            }

            const int buttonHeight = 30;
            const int buttonWidth = 200;
            if (selectedDescriptors.Length == 1 && !string.IsNullOrEmpty(selectedDescriptors[0].documentationUrl) && GUILayout.Button(Contents.Documentation, GUILayout.MaxWidth(buttonWidth), GUILayout.Height(buttonHeight)))
            {
                Application.OpenURL(selectedDescriptors[0].documentationUrl);
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
        }
    }
}
