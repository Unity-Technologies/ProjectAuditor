using System;
using System.Collections.Generic;
using System.Linq;
using Editor.Utils;
using Unity.ProjectAuditor.Editor.CodeAnalysis;
using Unity.ProjectAuditor.Editor.Utils;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using UnityEngine.Profiling;

namespace Unity.ProjectAuditor.Editor.UI
{
    class AnalysisViewDescriptor
    {
        public IssueCategory category;
        public string name;
        public int menuOrder;
        public bool groupByDescription;
        public bool descriptionWithIcon;
        public bool showAreaSelection;
        public bool showAssemblySelection;
        public bool showCritical;
        public bool showDependencyView;
        public bool showMuteOptions;
        public bool showRightPanels;
        public GUIContent dependencyViewGuiContent;
        public IssueLayout layout;
        public Action<Location> onDoubleClick;
        public Action<ProblemDescriptor> onOpenDescriptor;
        public ProjectAuditorAnalytics.UIButton analyticsEvent;
    }

    class AnalysisView
    {
        enum ExportMode
        {
            All = 0,
            Filtered = 1,
            Selected
        }

        ProjectAuditorConfig m_Config;
        Preferences m_Preferences;
        AnalysisViewDescriptor m_Desc;
        IProjectIssueFilter m_Filter;

        DependencyView m_DependencyView;
        List<ProjectIssue> m_Issues;
        IssueTable m_Table;

        public AnalysisViewDescriptor desc
        {
            get { return m_Desc; }
        }

        public IssueTable table
        {
            get { return m_Table; }
        }

        public void CreateTable(AnalysisViewDescriptor descriptor, ProjectAuditorConfig config, Preferences prefs, IProjectIssueFilter filter)
        {
            m_Desc = descriptor;
            m_Config = config;
            m_Preferences = prefs;
            m_Filter = filter;

            if (m_Table != null)
                return;

            var state = new TreeViewState();
            var columns = new MultiColumnHeaderState.Column[descriptor.layout.properties.Length];
            for (var i = 0; i < descriptor.layout.properties.Length; i++)
            {
                var property = descriptor.layout.properties[i];

                var width = 80;
                if (property.type == PropertyType.Description)
                    width = 300;
                else if (property.type == PropertyType.Severity)
                    width = 20;

                columns[i] = new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent(property.name, descriptor.layout.properties[i].longName),
                    width = width,
                    minWidth = 20,
                    autoResize = true
                };
            }

            m_Table = new IssueTable(state,
                new MultiColumnHeader(new MultiColumnHeaderState(columns)),
                m_Desc,
                m_Config,
                m_Filter);

            if (m_Desc.showDependencyView)
                m_DependencyView = new DependencyView(new TreeViewState(), m_Desc.onDoubleClick);
            m_Issues = new List<ProjectIssue>();
        }

        public void AddIssues(IEnumerable<ProjectIssue> allIssues)
        {
            var issues = allIssues.Where(i => i.category == m_Desc.category).ToArray();
            m_Issues.AddRange(issues);
            m_Table.AddIssues(issues);
        }

        public ProjectIssue[] GetIssues()
        {
            return m_Issues.ToArray();
        }

        public void Clear()
        {
            m_Issues.Clear();
            m_Table.Clear();
        }

        public void Refresh()
        {
            m_Table.Reload();
        }

        public bool IsValid()
        {
            return m_Table != null;
        }

        public void SetFlatView(bool value)
        {
            m_Table.SetFlatView(value);
        }

        public void OnGUI()
        {
            if (Styles.TextFieldWarning == null)
            {
                Styles.TextFieldWarning = new GUIStyle(EditorStyles.textField);
                Styles.TextFieldWarning.normal.textColor = Color.yellow;
            }

            if (Styles.TextArea == null)
                Styles.TextArea = new GUIStyle(EditorStyles.textArea);

            var selectedItems = m_Table.GetSelectedItems();
            var selectedIssues = selectedItems.Where(i => i.ProjectIssue != null).Select(i => i.ProjectIssue);
            var selectedDescriptors = selectedItems.Select(i => i.ProblemDescriptor).Distinct();

            EditorGUILayout.BeginHorizontal();

            DrawTable(selectedIssues.ToArray());

            if (m_Desc.showRightPanels)
            {
                DrawFoldouts(selectedDescriptors.ToArray());
            }

            EditorGUILayout.EndHorizontal();

            if (m_Desc.showDependencyView)
            {
                DrawDependencyView(selectedIssues.ToArray());
            }
        }

        void DrawTable(ProjectIssue[] selectedIssues)
        {
            EditorGUILayout.BeginVertical();
            EditorGUILayout.Space();

            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            EditorGUILayout.LabelField("Zoom", EditorStyles.label, GUILayout.ExpandWidth(false), GUILayout.Width(40));
            m_Preferences.fontSize = (int)GUILayout.HorizontalSlider(m_Preferences.fontSize, Preferences.k_MinFontSize, Preferences.k_MaxFontSize, GUILayout.ExpandWidth(false), GUILayout.Width(80));
            m_Table.SetFontSize(m_Preferences.fontSize);

            Styles.TextArea.fontSize = m_Preferences.fontSize;

            // (optional) collapse/expand buttons
            if (m_Desc.groupByDescription)
            {
                if (GUILayout.Button(Contents.CollapseAllButton, EditorStyles.toolbarButton, GUILayout.ExpandWidth(true), GUILayout.Width(100)))
                    SetRowsExpanded(false);
                if (GUILayout.Button(Contents.ExpandAllButton, EditorStyles.toolbarButton, GUILayout.ExpandWidth(true), GUILayout.Width(100)))
                    SetRowsExpanded(true);
            }

            EditorGUILayout.Space();

            if (Utility.ToolbarButtonWithDropdownList(Contents.ExportButton, k_ExportModeStrings,
                OnExport, GUILayout.Width(80)))
            {
                Export();

                GUIUtility.ExitGUI();
            }

            EditorGUILayout.EndHorizontal();

            var r = EditorGUILayout.GetControlRect(GUILayout.ExpandHeight(true));

            Profiler.BeginSample("IssueTable.OnGUI");
            m_Table.OnGUI(r);
            Profiler.EndSample();

            var info = selectedIssues.Length + " / " + m_Table.GetNumMatchingIssues() + " Items";
            EditorGUILayout.LabelField(info, GUILayout.ExpandWidth(true), GUILayout.Width(200));

            EditorGUILayout.EndVertical();
        }

        void DrawFoldouts(ProblemDescriptor[] selectedDescriptors)
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(LayoutSize.FoldoutWidth));

            DrawDetailsFoldout(selectedDescriptors);
            DrawRecommendationFoldout(selectedDescriptors);

            EditorGUILayout.EndVertical();
        }

        void DrawDetailsFoldout(ProblemDescriptor[] selectedDescriptors)
        {
            EditorGUILayout.BeginVertical(GUI.skin.box, GUILayout.Width(LayoutSize.FoldoutWidth));
            m_Preferences.details = Utility.BoldFoldout(m_Preferences.details, Contents.DetailsFoldout);
            if (m_Preferences.details)
            {
                if (selectedDescriptors.Length == 0)
                    GUILayout.TextArea(k_NoSelectionText, Styles.TextArea, GUILayout.MaxHeight(LayoutSize.FoldoutMaxHeight));
                else if (selectedDescriptors.Length > 1)
                    GUILayout.TextArea(k_MultipleSelectionText, Styles.TextArea, GUILayout.MaxHeight(LayoutSize.FoldoutMaxHeight));
                else // if (selectedDescriptors.Length == 1)
                    GUILayout.TextArea(selectedDescriptors[0].problem, Styles.TextArea, GUILayout.MaxHeight(LayoutSize.FoldoutMaxHeight));
            }
            EditorGUILayout.EndVertical();
        }

        void DrawRecommendationFoldout(ProblemDescriptor[] selectedDescriptors)
        {
            EditorGUILayout.BeginVertical(GUI.skin.box, GUILayout.Width(LayoutSize.FoldoutWidth));
            m_Preferences.recommendation = Utility.BoldFoldout(m_Preferences.recommendation, Contents.RecommendationFoldout);
            if (m_Preferences.recommendation)
            {
                if (selectedDescriptors.Length == 0)
                    GUILayout.TextArea(k_NoSelectionText, Styles.TextArea, GUILayout.MaxHeight(LayoutSize.FoldoutMaxHeight));
                else if (selectedDescriptors.Length > 1)
                    GUILayout.TextArea(k_MultipleSelectionText, Styles.TextArea, GUILayout.MaxHeight(LayoutSize.FoldoutMaxHeight));
                else // if (selectedDescriptors.Length == 1)
                    GUILayout.TextArea(selectedDescriptors[0].solution, Styles.TextArea, GUILayout.MaxHeight(LayoutSize.FoldoutMaxHeight));
            }
            EditorGUILayout.EndVertical();
        }

        void DrawDependencyView(ProjectIssue[] issues)
        {
            EditorGUILayout.BeginVertical(GUI.skin.box, GUILayout.Height(LayoutSize.DependencyViewHeight));

            m_Preferences.dependencies = Utility.BoldFoldout(m_Preferences.dependencies, m_Desc.dependencyViewGuiContent);
            if (m_Preferences.dependencies)
            {
                if (issues.Length == 0)
                {
                    EditorGUILayout.LabelField(k_NoSelectionText, Styles.TextArea, GUILayout.MaxHeight(LayoutSize.FoldoutMaxHeight));
                }
                else if (issues.Length > 1)
                {
                    EditorGUILayout.LabelField(k_MultipleSelectionText, Styles.TextArea, GUILayout.MaxHeight(LayoutSize.FoldoutMaxHeight));
                }
                else// if (issues.Length == 1)
                {
                    var selection = issues[0];
                    DependencyNode dependencies = null;
                    if (selection != null && selection.dependencies != null)
                    {
                        if (selection.dependencies as CallTreeNode != null)
                            dependencies = selection.dependencies.GetChild(); // skip self
                        else
                            dependencies = selection.dependencies;
                    }

                    m_DependencyView.SetRoot(dependencies);

                    if (dependencies != null)
                    {
                        var r = EditorGUILayout.GetControlRect(GUILayout.ExpandHeight(true));

                        m_DependencyView.OnGUI(r);
                    }
                    else
                    {
                        EditorGUILayout.LabelField(k_AnalysisIsRequiredText, Styles.TextArea, GUILayout.MaxHeight(LayoutSize.FoldoutMaxHeight));
                    }
                }
            }

            EditorGUILayout.EndVertical();
        }

        void SetRowsExpanded(bool expanded)
        {
            var rows = m_Table.GetRows();
            foreach (var row in rows)
                m_Table.SetExpanded(row.id, expanded);
        }

        void Export(Func<ProjectIssue, bool> match = null)
        {
            var path = EditorUtility.SaveFilePanel("Save to CSV file", "", string.Format("project-auditor-{0}.csv", m_Desc.category.ToString()).ToLower(),
                "csv");
            if (path.Length != 0)
            {
                var analytic = ProjectAuditorAnalytics.BeginAnalytic();
                using (var exporter = new Exporter(path, m_Desc.layout))
                {
                    exporter.WriteHeader();

                    var matchingIssues = m_Issues.Where(issue => m_Config.GetAction(issue.descriptor, issue.GetCallingMethod()) !=
                        Rule.Severity.None && (match == null || match(issue)));
                    foreach (var issue in matchingIssues)
                        exporter.WriteIssue(issue);
                }

                EditorUtility.RevealInFinder(path);

                ProjectAuditorAnalytics.SendUIButtonEvent(ProjectAuditorAnalytics.UIButton.Export, analytic);
            }
        }

        void OnExport(object data)
        {
            var mode = (ExportMode)data;
            switch (mode)
            {
                case ExportMode.All:
                    Export();
                    return;
                case ExportMode.Filtered:
                    Export(issue => { return m_Filter.Match(issue); });
                    return;
                case ExportMode.Selected:
                    var selectedItems = table.GetSelectedItems();
                    Export(issue =>
                    {
                        return selectedItems.Any(item => item.Find(issue));
                    });
                    return;
            }
        }

        const string k_NoSelectionText = "<No selection>";
        const string k_AnalysisIsRequiredText = "<Missing Data: Please Analyze>";
        const string k_MultipleSelectionText = "<Multiple selection>";

        static string[] k_ExportModeStrings =
        {
            "All",
            "Filtered",
            "Selected"
        };

        static class LayoutSize
        {
            public static readonly int FoldoutWidth = 300;
            public static readonly int FoldoutMaxHeight = 220;
            public static readonly int DependencyViewHeight = 200;
        }

        static class Contents
        {
            public static readonly GUIContent ExportButton = new GUIContent("Export", "Export project report to .csv files.");
            public static readonly GUIContent ExpandAllButton = new GUIContent("Expand All", "");
            public static readonly GUIContent CollapseAllButton = new GUIContent("Collapse All", "");
            public static readonly GUIContent DetailsFoldout = new GUIContent("Details", "Issue Details");
            public static readonly GUIContent RecommendationFoldout =
                new GUIContent("Recommendation", "Recommendation on how to solve the issue");
        }

        static class Styles
        {
            public static GUIStyle TextArea;
            public static GUIStyle TextFieldWarning;
        }
    }
}
