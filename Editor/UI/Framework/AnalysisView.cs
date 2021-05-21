using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.ProjectAuditor.Editor.CodeAnalysis;
using Unity.ProjectAuditor.Editor.Utils;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using UnityEngine.Profiling;

namespace Unity.ProjectAuditor.Editor.UI.Framework
{
    public class AnalysisView
    {
        static string s_ExportDirectory = string.Empty;

        public static Action<IssueCategory> OnChangeView;

        protected static ProjectReport s_Report;

        enum ExportMode
        {
            All = 0,
            Filtered = 1,
            Selected
        }

        protected ProjectAuditorConfig m_Config;
        protected Preferences m_Preferences;
        protected ViewDescriptor m_Desc;
        protected IProjectIssueFilter m_Filter;
        protected List<ProjectIssue> m_Issues = new List<ProjectIssue>();

        DependencyView m_DependencyView;
        bool m_FlatView;
        IssueTable m_Table;
        IssueLayout m_Layout;

        public ViewDescriptor desc
        {
            get { return m_Desc; }
        }

        protected int numIssues
        {
            get
            {
                return m_Issues.Count();
            }
        }

        internal IssueTable table
        {
            get { return m_Table; }
        }

        public virtual void Create(ViewDescriptor descriptor, IssueLayout layout, ProjectAuditorConfig config, Preferences prefs, IProjectIssueFilter filter)
        {
            m_Desc = descriptor;
            m_Config = config;
            m_Preferences = prefs;
            m_Filter = filter;
            m_Layout = layout;

            if (m_Table != null)
                return;

            var state = new TreeViewState();
            var columns = new MultiColumnHeaderState.Column[layout.properties.Length];
            for (var i = 0; i < layout.properties.Length; i++)
            {
                var property = layout.properties[i];

                var width = 80;
                switch (property.type)
                {
                    case PropertyType.Description:
                        width = 300;
                        break;
                    case PropertyType.Path:
                        width = 500;
                        break;
                    case PropertyType.Severity:
                        width = 24;
                        break;
                }

                columns[i] = new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent(property.name, layout.properties[i].longName),
                    width = width,
                    minWidth = 20,
                    autoResize = true
                };
            }

            m_Table = new IssueTable(state,
                new MultiColumnHeader(new MultiColumnHeaderState(columns)),
                m_Desc,
                layout,
                m_Config,
                m_Filter);

            if (m_Desc.showDependencyView)
                m_DependencyView = new DependencyView(new TreeViewState(), m_Desc.onDoubleClick);

            SetFlatView(m_FlatView);
        }

        public virtual void AddIssues(IEnumerable<ProjectIssue> allIssues)
        {
            var issues = allIssues.Where(i => i.category == m_Desc.category).ToArray();
            m_Issues.AddRange(issues);
            m_Table.AddIssues(issues);
        }

        protected ProjectIssue[] GetIssues()
        {
            return m_Issues.ToArray();
        }

        public virtual void Clear()
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

        void SetFlatView(bool value)
        {
            m_Table.SetFlatView(value);
        }

        public virtual void DrawFilters()
        {
        }

        public virtual void DrawContent()
        {
            var selectedItems = m_Table.GetSelectedItems();
            var selectedIssues = selectedItems.Where(i => i.ProjectIssue != null).Select(i => i.ProjectIssue).ToArray();
            var selectedDescriptors = selectedItems.Select(i => i.ProblemDescriptor).Distinct().ToArray();

            EditorGUILayout.BeginHorizontal();

            DrawTable(selectedIssues);

            if (m_Desc.showRightPanels)
            {
                DrawFoldouts(selectedDescriptors);
            }

            EditorGUILayout.EndHorizontal();

            if (m_Desc.showDependencyView)
            {
                DrawDependencyView(selectedIssues);
            }
        }

        public void DrawInfo()
        {
            if (!m_Desc.showInfoPanel)
                return;

            EditorGUILayout.BeginVertical(GUI.skin.box, GUILayout.ExpandWidth(true));

            m_Preferences.info = Utility.BoldFoldout(m_Preferences.info, Contents.InfoFoldout);
            if (m_Preferences.info)
            {
                EditorGUI.indentLevel++;

                OnDrawInfo();

                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndVertical();
        }

        protected virtual void OnDrawInfo()
        {
        }

        void DrawTable(ProjectIssue[] selectedIssues)
        {
            EditorGUILayout.BeginVertical();
            EditorGUILayout.Space();

            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            DrawViewOptions();

            EditorGUILayout.Space();

            DrawDataOptions();

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
                    GUILayout.TextArea(k_NoSelectionText, SharedStyles.TextArea, GUILayout.MaxHeight(LayoutSize.FoldoutMaxHeight));
                else if (selectedDescriptors.Length > 1)
                    GUILayout.TextArea(k_MultipleSelectionText, SharedStyles.TextArea, GUILayout.MaxHeight(LayoutSize.FoldoutMaxHeight));
                else // if (selectedDescriptors.Length == 1)
                    GUILayout.TextArea(selectedDescriptors[0].problem, SharedStyles.TextArea, GUILayout.MaxHeight(LayoutSize.FoldoutMaxHeight));
            }
            EditorGUILayout.EndVertical();
        }

        void DrawViewOptions()
        {
            EditorGUILayout.LabelField("Zoom", EditorStyles.label, GUILayout.ExpandWidth(false), GUILayout.Width(40));
            m_Preferences.fontSize = (int)GUILayout.HorizontalSlider(m_Preferences.fontSize, Preferences.k_MinFontSize, Preferences.k_MaxFontSize, GUILayout.ExpandWidth(false), GUILayout.Width(80));
            m_Table.SetFontSize(m_Preferences.fontSize);

            SharedStyles.TextArea.fontSize = m_Preferences.fontSize;

            if (m_Desc.groupByDescriptor)
            {
                // (optional) collapse/expand buttons
                GUI.enabled = !m_FlatView;
                if (GUILayout.Button(Contents.CollapseAllButton, EditorStyles.toolbarButton, GUILayout.ExpandWidth(true), GUILayout.Width(100)))
                    SetRowsExpanded(false);
                if (GUILayout.Button(Contents.ExpandAllButton, EditorStyles.toolbarButton, GUILayout.ExpandWidth(true), GUILayout.Width(100)))
                    SetRowsExpanded(true);
                GUI.enabled = true;

                EditorGUI.BeginChangeCheck();
                m_FlatView = GUILayout.Toggle(m_FlatView, "Flat View", EditorStyles.toolbarButton, GUILayout.Width(100));
                if (EditorGUI.EndChangeCheck())
                {
                    SetFlatView(m_FlatView);
                    Refresh();
                }
            }
        }

        void DrawDataOptions()
        {
            if (m_Desc.onDrawToolbarDataOptions != null)
                m_Desc.onDrawToolbarDataOptions();

            if (Utility.ToolbarButtonWithDropdownList(Contents.ExportButton, k_ExportModeStrings,
                OnExport, GUILayout.Width(80)))
            {
                Export();

                GUIUtility.ExitGUI();
            }
        }

        void DrawRecommendationFoldout(ProblemDescriptor[] selectedDescriptors)
        {
            EditorGUILayout.BeginVertical(GUI.skin.box, GUILayout.Width(LayoutSize.FoldoutWidth));
            m_Preferences.recommendation = Utility.BoldFoldout(m_Preferences.recommendation, Contents.RecommendationFoldout);
            if (m_Preferences.recommendation)
            {
                if (selectedDescriptors.Length == 0)
                    GUILayout.TextArea(k_NoSelectionText, SharedStyles.TextArea, GUILayout.MaxHeight(LayoutSize.FoldoutMaxHeight));
                else if (selectedDescriptors.Length > 1)
                    GUILayout.TextArea(k_MultipleSelectionText, SharedStyles.TextArea, GUILayout.MaxHeight(LayoutSize.FoldoutMaxHeight));
                else // if (selectedDescriptors.Length == 1)
                    GUILayout.TextArea(selectedDescriptors[0].solution, SharedStyles.TextArea, GUILayout.MaxHeight(LayoutSize.FoldoutMaxHeight));
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
                    EditorGUILayout.LabelField(k_NoSelectionText, SharedStyles.TextArea, GUILayout.MaxHeight(LayoutSize.FoldoutMaxHeight));
                }
                else if (issues.Length > 1)
                {
                    EditorGUILayout.LabelField(k_MultipleSelectionText, SharedStyles.TextArea, GUILayout.MaxHeight(LayoutSize.FoldoutMaxHeight));
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
                        EditorGUILayout.LabelField(k_AnalysisIsRequiredText, SharedStyles.TextArea, GUILayout.MaxHeight(LayoutSize.FoldoutMaxHeight));
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
            var path = EditorUtility.SaveFilePanel("Save to CSV file", s_ExportDirectory, string.Format("project-auditor-{0}.csv", m_Desc.category.ToString()).ToLower(),
                "csv");
            if (path.Length != 0)
            {
                var analytic = ProjectAuditorAnalytics.BeginAnalytic();
                using (var exporter = new Exporter(path, m_Layout))
                {
                    exporter.WriteHeader();

                    var matchingIssues = m_Issues.Where(issue => m_Config.GetAction(issue.descriptor, issue.GetCallingMethod()) !=
                        Rule.Severity.None && (match == null || match(issue)));
                    exporter.WriteIssues(matchingIssues.ToArray());
                }

                EditorUtility.RevealInFinder(path);

                ProjectAuditorAnalytics.SendUIButtonEvent(ProjectAuditorAnalytics.UIButton.Export, analytic);

                s_ExportDirectory = Path.GetDirectoryName(path);
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

        public static void SetReport(ProjectReport report)
        {
            s_Report = report;
        }

        const string k_NoSelectionText = "<No selection>";
        const string k_AnalysisIsRequiredText = "<Missing Data: Please Analyze>";
        const string k_MultipleSelectionText = "<Multiple selection>";

        static readonly string[] k_ExportModeStrings =
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
            public static readonly GUIContent ExportButton = new GUIContent("Export", "Export current view to .csv file");
            public static readonly GUIContent ExpandAllButton = new GUIContent("Expand All");
            public static readonly GUIContent CollapseAllButton = new GUIContent("Collapse All");

            public static readonly GUIContent InfoFoldout = new GUIContent("Information");
            public static readonly GUIContent DetailsFoldout = new GUIContent("Details", "Issue Details");
            public static readonly GUIContent RecommendationFoldout =
                new GUIContent("Recommendation", "Recommendation on how to solve the issue");
        }
    }
}
