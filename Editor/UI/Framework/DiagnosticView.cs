using System;
using System.IO;
using System.Linq;
using Unity.ProjectAuditor.Editor.Core;
using Unity.ProjectAuditor.Editor.Diagnostic;
using Unity.ProjectAuditor.Editor.Utils;
using UnityEditor;
using UnityEngine;

namespace Unity.ProjectAuditor.Editor.UI.Framework
{
	internal class DiagnosticView : AnalysisView
    {

        public override string description => $"A list of {m_Desc.displayName} issues found in the project.";

        Vector2 m_DetailsScrollPos;
        Vector2 m_RecommendationScrollPos;

        public DiagnosticView(ViewManager viewManager) : base(viewManager)
        {
        }

        public override void DrawDetails(ProjectIssue[] selectedIssues)
        {
            var selectedDescriptors = selectedIssues.Select(i => i.descriptor).Distinct().ToArray();

            EditorGUILayout.BeginVertical(GUILayout.Width(LayoutSize.FoldoutWidth));


            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(Contents.Details, SharedStyles.BoldLabel);
                {
                    if (selectedDescriptors.Length != 0)
                    {
                        if (GUILayout.Button(Contents.CopyToClipboard, SharedStyles.DarkSmallButton,
                            GUILayout.Width(LayoutSize.CopyToClipboardButtonSize),
                            GUILayout.Height(LayoutSize.CopyToClipboardButtonSize)))
                        {
                            EditorInterop.CopyToClipboard(Formatting.StripRichTextTags(selectedDescriptors[0].description));
                        }
                    }
                }
            }

            m_DetailsScrollPos =
                EditorGUILayout.BeginScrollView(m_DetailsScrollPos, GUILayout.ExpandHeight(true));

            if (selectedDescriptors.Length == 0)
                GUILayout.TextArea(k_NoSelectionText, SharedStyles.TextAreaWithDynamicSize,
                    GUILayout.MaxHeight(LayoutSize.FoldoutMaxHeight));
            else if (selectedDescriptors.Length > 1)
                GUILayout.TextArea(k_MultipleSelectionText, SharedStyles.TextAreaWithDynamicSize,
                    GUILayout.MaxHeight(LayoutSize.FoldoutMaxHeight));
            else
                GUILayout.TextArea(selectedDescriptors[0].description, SharedStyles.TextAreaWithDynamicSize,
                    GUILayout.MaxHeight(LayoutSize.FoldoutMaxHeight));

            EditorGUILayout.EndScrollView();

            ChartUtil.DrawLine(m_2D);
            GUILayout.Space(10);

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(Contents.Recommendation, SharedStyles.BoldLabel);
                {
                    if (selectedDescriptors.Length != 0)
                    {
                        if (GUILayout.Button(Contents.CopyToClipboard, SharedStyles.DarkSmallButton,
                            GUILayout.Width(LayoutSize.CopyToClipboardButtonSize),
                            GUILayout.Height(LayoutSize.CopyToClipboardButtonSize)))
                        {
                            EditorInterop.CopyToClipboard(
                                Formatting.StripRichTextTags(selectedDescriptors[0].solution));
                        }
                    }
                }
            }

            m_RecommendationScrollPos =
                EditorGUILayout.BeginScrollView(m_RecommendationScrollPos, GUILayout.ExpandHeight(true));

            if (selectedDescriptors.Length == 0)
                GUILayout.TextArea(k_NoSelectionText, SharedStyles.TextAreaWithDynamicSize,
                    GUILayout.MaxHeight(LayoutSize.FoldoutMaxHeight));
            else if (selectedDescriptors.Length > 1)
                GUILayout.TextArea(k_MultipleSelectionText, SharedStyles.TextAreaWithDynamicSize,
                    GUILayout.MaxHeight(LayoutSize.FoldoutMaxHeight));
            else
                GUILayout.TextArea(selectedDescriptors[0].solution, SharedStyles.TextAreaWithDynamicSize,
                    GUILayout.MaxHeight(LayoutSize.FoldoutMaxHeight));

            EditorGUILayout.EndScrollView();

            if (selectedDescriptors.Length == 1)
            {
                if (!IsIssueIgnored(selectedIssues))
                {
                    DrawActionButton(Contents.Ignore, () =>
                    {
                        IgnoreIssue(selectedIssues[0], Severity.None);

                        if (m_ViewManager.onIgnoreIssues != null)
                            m_ViewManager.onIgnoreIssues(selectedIssues);
                    });
                }

                if (IsIssueIgnored(selectedIssues))
                {
                    DrawActionButton(Contents.Display, () =>
                    {
                        m_Config.ClearRules(selectedDescriptors[0], selectedIssues[0].GetContext());

                        if (m_ViewManager.onDisplayIssues != null)
                            m_ViewManager.onDisplayIssues(selectedIssues);
                    });
                }

                if (!string.IsNullOrEmpty(selectedDescriptors[0].documentationUrl))
                {
                    DrawActionButton(Contents.Documentation, () =>
                    {
                        Application.OpenURL(selectedDescriptors[0].documentationUrl);

                        if (m_ViewManager.onShowDocumentation != null)
                            m_ViewManager.onShowDocumentation(selectedDescriptors[0].documentationUrl);
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

                        if (m_ViewManager.onQuickFixIssues != null)
                            m_ViewManager.onQuickFixIssues(selectedIssues);
                    });

                    GUI.enabled = true;
                }
            }

            if (selectedDescriptors.Length > 1)
            {
                if (!IsIssueIgnored(selectedIssues))
                {
                    DrawActionButton(Contents.IgnoreAll, () =>
                    {
                        for (int i = 0; i < selectedIssues.Length; i++)
                        {
                            IgnoreIssue(selectedIssues[i], Severity.None);
                        }

                        if (m_ViewManager.onIgnoreIssues != null)
                            m_ViewManager.onIgnoreIssues(selectedIssues);
                    });
                }
                else
                {
                    DrawActionButton(Contents.DisplayAll, () =>
                    {
                        for (int i = 0; i < selectedIssues.Length; i++)
                        {
                            DisplayIssues(selectedIssues[i]);
                        }

                        if (m_ViewManager.onDisplayIssues != null)
                            m_ViewManager.onDisplayIssues(selectedIssues);
                    });
                }
            }

            EditorGUILayout.EndVertical();
        }

        void IgnoreIssue(ProjectIssue issue, Severity ruleSeverity)
        {
            var descriptor = issue.descriptor;
            var context = issue.GetContext();
            var rule = m_Config.GetRule(descriptor, context);

            if (rule == null)
                m_Config.AddRule(new Rule
                {
                    id = descriptor.id,
                    filter = context,
                    severity = ruleSeverity
                });
            else
                rule.severity = ruleSeverity;
        }

        void DisplayIssues(ProjectIssue issue)
        {
            m_Config.ClearRules(issue.descriptor, issue.GetContext());
        }

        bool IsIssueIgnored(ProjectIssue[] selectedIssues)
        {
            for (int i = 0; i < selectedIssues.Length; i++)
            {
                var descriptor = selectedIssues[i].descriptor;
                var context = selectedIssues[i].GetContext();
                var rule = m_Config.GetRule(descriptor, context);

                //If at least one issue in the selection is not ignored, consider the whole selection as not ignored
                if (rule == null) return false;
            }

            return true;
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
            public static readonly GUIContent Details = new GUIContent("Details", "Issue Details");
            public static readonly GUIContent Recommendation =
                new GUIContent("Recommendation", "Recommendation on how to solve the issue");
            public static readonly GUIContent Documentation = new GUIContent("Documentation", "Open the Unity documentation");
            public static readonly GUIContent QuickFix = new GUIContent("Quick Fix", "Automatically fix the issue");
            public static readonly GUIContent Ignore = new GUIContent("Ignore", "Always ignore selected issue.");
            public static readonly GUIContent IgnoreAll = new GUIContent("Ignore All", "Always ignore selected issues.");
            public static readonly GUIContent Display = new GUIContent("Display", "Always show selected issue.");
            public static readonly GUIContent DisplayAll = new GUIContent("Display All", "Always show selected issues.");
            public static readonly GUIContent CopyToClipboard = Utility.GetIcon(Utility.IconType.CopyToClipboard);
        }
    }
}
