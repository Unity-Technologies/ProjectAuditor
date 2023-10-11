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
            Descriptor descriptor = null;
            var selectedIDs = selectedIssues.Select(i => i.id).Distinct().ToArray();
            var numSelectedIDs = selectedIDs.Length;
            if (numSelectedIDs > 0)
            {
                descriptor = selectedIDs[0].GetDescriptor();
            }

            EditorGUILayout.BeginVertical(GUILayout.Width(LayoutSize.FoldoutWidth));

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(Contents.Details, SharedStyles.BoldLabel);
                {
                    if (numSelectedIDs != 0)
                    {
                        if (GUILayout.Button(Contents.CopyToClipboard, SharedStyles.TabButton,
                            GUILayout.Width(LayoutSize.CopyToClipboardButtonSize),
                            GUILayout.Height(LayoutSize.CopyToClipboardButtonSize)))
                        {
                            EditorInterop.CopyToClipboard(Formatting.StripRichTextTags(descriptor.description));
                        }
                    }
                }
            }

            m_DetailsScrollPos =
                EditorGUILayout.BeginScrollView(m_DetailsScrollPos, GUILayout.ExpandHeight(true));

            if (numSelectedIDs == 0)
                GUILayout.TextArea(k_NoSelectionText, SharedStyles.TextAreaWithDynamicSize,
                    GUILayout.MaxHeight(LayoutSize.FoldoutMaxHeight));
            else if (numSelectedIDs > 1)
                GUILayout.TextArea(k_MultipleSelectionText, SharedStyles.TextAreaWithDynamicSize,
                    GUILayout.MaxHeight(LayoutSize.FoldoutMaxHeight));
            else
            {
                GUILayout.TextArea(descriptor.description, SharedStyles.TextAreaWithDynamicSize,
                    GUILayout.MaxHeight(LayoutSize.FoldoutMaxHeight));
            }

            EditorGUILayout.EndScrollView();

            ChartUtil.DrawLine(m_2D);
            GUILayout.Space(10);

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(Contents.Recommendation, SharedStyles.BoldLabel);
                {
                    if (numSelectedIDs != 0)
                    {
                        if (GUILayout.Button(Contents.CopyToClipboard, SharedStyles.TabButton,
                            GUILayout.Width(LayoutSize.CopyToClipboardButtonSize),
                            GUILayout.Height(LayoutSize.CopyToClipboardButtonSize)))
                        {
                            EditorInterop.CopyToClipboard(Formatting.StripRichTextTags(descriptor.solution));
                        }
                    }
                }
            }

            m_RecommendationScrollPos =
                EditorGUILayout.BeginScrollView(m_RecommendationScrollPos, GUILayout.ExpandHeight(true));

            if (numSelectedIDs == 0)
                GUILayout.TextArea(k_NoSelectionText, SharedStyles.TextAreaWithDynamicSize,
                    GUILayout.MaxHeight(LayoutSize.FoldoutMaxHeight));
            else if (numSelectedIDs > 1)
                GUILayout.TextArea(k_MultipleSelectionText, SharedStyles.TextAreaWithDynamicSize,
                    GUILayout.MaxHeight(LayoutSize.FoldoutMaxHeight));
            else
            {
                GUILayout.TextArea(descriptor.solution, SharedStyles.TextAreaWithDynamicSize,
                    GUILayout.MaxHeight(LayoutSize.FoldoutMaxHeight));
            }

            EditorGUILayout.EndScrollView();

            var issuesAreIgnored = AreIssuesIgnored(selectedIssues);
            if (numSelectedIDs == 1)
            {
                if (!string.IsNullOrEmpty(descriptor.documentationUrl))
                {
                    DrawActionButton(Contents.Documentation, () =>
                    {
                        Application.OpenURL(descriptor.documentationUrl);

                        m_ViewManager.OnSelectedIssuesDocumentationRequested?.Invoke(selectedIssues);
                    });
                }

                if (descriptor.fixer != null)
                {
                    GUI.enabled = selectedIssues.Any(i => !i.wasFixed);

                    DrawActionButton(Contents.QuickFix, () =>
                    {
                        foreach (var issue in selectedIssues)
                        {
                            descriptor.Fix(issue);
                        }

                        m_ViewManager.OnSelectedIssuesQuickFixRequested?.Invoke(selectedIssues);
                    });

                    GUI.enabled = true;
                }
            }

            if (selectedIssues.Length > 0)
            {
                if (issuesAreIgnored)
                {
                    DrawActionButton(selectedIssues.Length > 1 ? Contents.DisplayAll : Contents.Display, () =>
                    {
                        foreach (var t in selectedIssues)
                        {
                            m_Rules.ClearRules(t);
                        }

                        m_ViewManager.OnSelectedIssuesDisplayRequested?.Invoke(selectedIssues);

                        ClearSelection();
                    });
                }
                else
                {
                    DrawActionButton(selectedIssues.Length > 1 ? Contents.IgnoreAll : Contents.Ignore, () =>
                    {
                        foreach (var t in selectedIssues)
                        {
                            m_Rules.SetRule(t, Severity.None);
                        }

                        m_ViewManager.OnSelectedIssuesIgnoreRequested?.Invoke(selectedIssues);

                        ClearSelection();
                    });
                }
            }

            EditorGUILayout.EndVertical();
        }

        public override void DrawFilters()
        {
            EditorGUI.BeginChangeCheck();

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("Show :", GUILayout.ExpandWidth(true), GUILayout.Width(80));

                var wasShowingCritical = m_ViewStates.onlyCriticalIssues;
                m_ViewStates.onlyCriticalIssues = EditorGUILayout.ToggleLeft("Only Major/Critical",
                    m_ViewStates.onlyCriticalIssues, GUILayout.Width(170));

                if (wasShowingCritical != m_ViewStates.onlyCriticalIssues)
                    m_ViewManager.OnMajorOrCriticalIssuesVisibilityChanged?.Invoke(m_ViewStates.onlyCriticalIssues);
            }

            if (EditorGUI.EndChangeCheck())
                MarkDirty();
        }

        protected override void DrawInfo()
        {
            EditorGUILayout.LabelField("\u2022 Use the Filters to reduce the number of reported issues");
            EditorGUILayout.LabelField("\u2022 Use the Ignore button to mark an issue as false-positive");
        }

        public override void DrawViewOptions()
        {
            base.DrawViewOptions();

            var guiContent = m_Table.showIgnoredIssues
                ? Contents.ShowIgnoredIssuesButton
                : Contents.HideIgnoredIssuesButton;

            DrawToolbarLargeButton(guiContent, () =>
            {
                m_Table.showIgnoredIssues = !m_Table.showIgnoredIssues;
                m_ViewManager.OnIgnoredIssuesVisibilityChanged?.Invoke(m_Table.showIgnoredIssues);
                MarkDirty();
            });
        }

        bool AreIssuesIgnored(ProjectIssue[] selectedIssues)
        {
            foreach (var issue in selectedIssues)
            {
                var context = issue.GetContext();
                var rule = m_Rules.GetRule(issue.id, context);

                //If at least one issue in the selection is not ignored, consider the whole selection as not ignored
                if (rule == null)
                    return false;
            }

            return true;
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
                    matchingIssues = matchingIssues.Where(issue => issue.id.IsValid() || m_Rules.GetAction(issue.id, issue.GetContext()) != Severity.None);
                    exporter.WriteIssues(matchingIssues.ToArray());
                }

                EditorUtility.RevealInFinder(path);

                m_ViewManager.OnViewExportCompleted?.Invoke();

                UserPreferences.loadSavePath = Path.GetDirectoryName(path);
            }
        }

        public override bool Match(ProjectIssue issue)
        {
            if (!base.Match(issue))
                return false;

            if (m_Table.showIgnoredIssues)
                return true;

            var id = issue.id;
            if (!id.IsValid())
                return true;

            var context = issue.GetContext();

            return m_Rules.GetAction(id, context) != Severity.None;
        }

        static class Contents
        {
            public static readonly GUIContent Details = new GUIContent("Details", "Issue Details");
            public static readonly GUIContent Recommendation =
                new GUIContent("Recommendation", "Recommendation on how to solve the issue");
            public static readonly GUIContent Documentation = new GUIContent("Documentation", "Open the Unity documentation");
            public static readonly GUIContent QuickFix = new GUIContent("Quick Fix", "Automatically fix the issue");
            public static readonly GUIContent ShowIgnoredIssuesButton = Utility.GetDisplayIgnoredIssuesIconWithLabel();
            public static readonly GUIContent HideIgnoredIssuesButton = Utility.GetHiddenIgnoredIssuesIconWithLabel();
            public static readonly GUIContent Ignore = new GUIContent("Ignore", "Always ignore selected issue");
            public static readonly GUIContent IgnoreAll = new GUIContent("Ignore All", "Always ignore selected issues");
            public static readonly GUIContent Display = new GUIContent("Display", "Always show selected issue");
            public static readonly GUIContent DisplayAll = new GUIContent("Display All", "Always show selected issues");
            public static readonly GUIContent CopyToClipboard = Utility.GetIcon(Utility.IconType.CopyToClipboard);
        }
    }
}
