using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.ProjectAuditor.Editor.CodeAnalysis;
using Unity.ProjectAuditor.Editor.Core;
using Unity.ProjectAuditor.Editor.Utils;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using UnityEngine.Profiling;

namespace Unity.ProjectAuditor.Editor.UI.Framework
{
    public class AnalysisView : IProjectIssueFilter
    {
        enum ExportMode
        {
            All = 0,
            Filtered = 1,
            Selected
        }

        protected Draw2D m_2D;
        protected bool m_Dirty = true;
        protected ProjectAuditorConfig m_Config;
        protected ProjectAuditorModule m_Module;
        protected ViewStates m_ViewStates;
        protected ViewDescriptor m_Desc;
        protected IProjectIssueFilter m_BaseFilter;
        protected List<ProjectIssue> m_Issues = new List<ProjectIssue>();
        protected TextFilter m_TextFilter;
        protected ViewManager m_ViewManager;

        DependencyView m_DependencyView;
        bool m_ShowInfo;
        bool m_ShowWarn;
        bool m_ShowError;
        GUIContent m_HelpButtonContent;
        IssueTable m_Table;
        IssueLayout m_Layout;
        Utility.DropdownItem[] m_GroupDropdownItems;

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

        internal ViewManager viewManager
        {
            get { return m_ViewManager; }
        }

        public AnalysisView(ViewManager viewManager)
        {
            m_2D = new Draw2D("Unlit/ProjectAuditor");
            m_ShowInfo = m_ShowWarn = m_ShowError = true;

            m_ViewManager = viewManager;
        }

        public virtual void Create(ViewDescriptor descriptor, IssueLayout layout, ProjectAuditorConfig config, ProjectAuditorModule module, ViewStates viewStates, IProjectIssueFilter filter)
        {
            m_Desc = descriptor;
            m_Config = config;
            m_Module = module;
            m_ViewStates = viewStates;
            m_BaseFilter = filter;
            m_Layout = layout;

            m_GroupDropdownItems = m_Layout.properties.Select(p => new Utility.DropdownItem
            {
                Content = new GUIContent(p.defaultGroup ? p.name + " (default)" : p.name),
                SelectionContent = new GUIContent("Group By: " + p.name),
                Enabled = p.format == PropertyFormat.String || p.format == PropertyFormat.Bool || p.format == PropertyFormat.Integer,
                UserData = Array.IndexOf(m_Layout.properties, p)
            }).ToArray();

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
                this);

            if (m_Desc.showDependencyView)
                m_DependencyView = new DependencyView(new TreeViewState(), m_Desc.onOpenIssue);

            if (m_TextFilter == null)
                m_TextFilter = new TextFilter(layout.properties);

            var helpButtonTooltip = string.Format("Open Reference for {0}", m_Desc.name);
            m_HelpButtonContent = Utility.GetIcon(Utility.IconType.Help, helpButtonTooltip);
        }

        public virtual void AddIssues(IEnumerable<ProjectIssue> allIssues)
        {
            var issues = allIssues.Where(i => i.category == m_Desc.category).ToArray();
            if (issues.Length == 0)
                return;

            m_Issues.AddRange(issues);
            m_Table.AddIssues(issues);

            m_Dirty = true;
        }

        public virtual void Clear()
        {
            m_Issues.Clear();
            m_Table.Clear();

            m_Dirty = true;
        }

        /// <summary>
        /// Mark view as dirty. Use this to force a table reload.
        /// </summary>
        public void MarkDirty()
        {
            m_Dirty = true;
        }

        void RefreshIfDirty()
        {
            if (!m_Dirty)
                return;

            m_Table.Reload();
            m_Dirty = false;
        }

        public bool IsValid()
        {
            return m_Table != null;
        }

        public virtual void DrawFilters()
        {
        }

        public virtual void DrawContent()
        {
            var selectedItems = m_Table.GetSelectedItems();
            var selectedIssues = selectedItems.Where(i => i.ProjectIssue != null).Select(i => i.ProjectIssue).ToArray();

            EditorGUILayout.BeginHorizontal();

            DrawTable(selectedIssues);

            if (m_Desc.showRightPanels)
            {
                DrawRightPanels(selectedIssues);
            }

            EditorGUILayout.EndHorizontal();

            if (m_Desc.showDependencyView)
            {
                DrawDependencyView(selectedIssues);
            }
        }

        public void DrawTopPanel()
        {
            if (!m_Desc.showInfoPanel)
                return;

            EditorGUILayout.BeginVertical(GUI.skin.box, GUILayout.ExpandWidth(true));

            m_ViewStates.info = Utility.BoldFoldout(m_ViewStates.info, Contents.InfoFoldout);
            if (m_ViewStates.info)
            {
                EditorGUI.indentLevel++;

                DrawInfo();

                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndVertical();
        }

        protected virtual void DrawInfo()
        {
        }

        void DrawTable(ProjectIssue[] selectedIssues)
        {
            RefreshIfDirty();

            EditorGUILayout.BeginVertical();
            EditorGUILayout.Space();

            DrawToolbar();

            var r = EditorGUILayout.GetControlRect(GUILayout.ExpandHeight(true));

            Profiler.BeginSample("IssueTable.OnGUI");
            m_Table.OnGUI(r);
            Profiler.EndSample();

            var info = selectedIssues.Length + " / " + m_Table.GetNumMatchingIssues() + " Items selected";
            EditorGUILayout.LabelField(info, GUILayout.ExpandWidth(true), GUILayout.Width(200));

            EditorGUILayout.EndVertical();
        }

        public virtual void DrawSearch()
        {
            EditorGUILayout.BeginHorizontal();

            // note that we don't need to detect string changes (with EditorGUI.Begin/EndChangeCheck(), because the TreeViewController already triggers a BuildRows() when the text changes
            EditorGUILayout.LabelField(Contents.SearchStringLabel, GUILayout.Width(80));

            m_TextFilter.searchString = EditorGUILayout.DelayedTextField(m_TextFilter.searchString, GUILayout.Width(180));
            m_Table.searchString = m_TextFilter.searchString;

            EditorGUI.BeginChangeCheck();
            m_TextFilter.ignoreCase = !EditorGUILayout.ToggleLeft(Contents.TextSearchCaseSensitive, !m_TextFilter.ignoreCase, GUILayout.Width(160));

            if (UserPreferences.developerMode && m_Desc.showDependencyView)
            {
                // this is only available in developer mode because it is still too slow at the moment
                m_TextFilter.searchDependencies = EditorGUILayout.ToggleLeft("Dependencies (might be slow)",
                    m_TextFilter.searchDependencies, GUILayout.Width(160));
            }

            if (EditorGUI.EndChangeCheck())
                MarkDirty();

            EditorGUILayout.EndHorizontal();
        }

        void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            DrawViewOptions();

            EditorGUILayout.Space();

            DrawDataOptions();

            Utility.DrawHelpButton(m_HelpButtonContent, new string(m_Desc.name.Where(char.IsLetterOrDigit).ToArray()));

            EditorGUILayout.EndHorizontal();
        }

        public virtual void DrawRightPanels(ProjectIssue[] selectedIssues)
        {
            var selectedDescriptors = selectedIssues.Select(i => i.descriptor).Distinct().ToArray();

            EditorGUILayout.BeginVertical(GUILayout.Width(LayoutSize.FoldoutWidth));

            DrawDetails(selectedDescriptors);
            DrawRecommendation(selectedDescriptors);

            EditorGUILayout.EndVertical();
        }

        void DrawDetails(ProblemDescriptor[] selectedDescriptors)
        {
            EditorGUILayout.BeginVertical(GUI.skin.box, GUILayout.Width(LayoutSize.FoldoutWidth));
            m_ViewStates.details = Utility.BoldFoldout(m_ViewStates.details, Contents.DetailsFoldout);
            if (m_ViewStates.details)
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
            if (!m_Module.isEnabledByDefault && m_ViewManager.onAnalyze != null && GUILayout.Button(Contents.AnalyzeNow, EditorStyles.toolbarButton, GUILayout.Width(120)))
            {
                m_ViewManager.onAnalyze(m_Module);
            }

            EditorGUILayout.LabelField(Utility.GetIcon(Utility.IconType.ZoomTool), EditorStyles.label, GUILayout.ExpandWidth(false), GUILayout.Width(20));
            m_ViewStates.fontSize = (int)GUILayout.HorizontalSlider(m_ViewStates.fontSize, ViewStates.k_MinFontSize, ViewStates.k_MaxFontSize, GUILayout.ExpandWidth(false), GUILayout.Width(AnalysisView.toolbarButtonSize));
            m_Table.SetFontSize(m_ViewStates.fontSize);

            SharedStyles.Label.fontSize = m_ViewStates.fontSize;
            SharedStyles.TextArea.fontSize = m_ViewStates.fontSize;

            if (!m_Layout.hierarchy)
            {
                // (optional) collapse/expand buttons
                GUI.enabled = !m_Table.flatView;
                if (GUILayout.Button(Contents.CollapseAllButton, EditorStyles.toolbarButton, GUILayout.ExpandWidth(true), GUILayout.Width(AnalysisView.toolbarButtonSize)))
                    SetRowsExpanded(false);
                if (GUILayout.Button(Contents.ExpandAllButton, EditorStyles.toolbarButton, GUILayout.ExpandWidth(true), GUILayout.Width(AnalysisView.toolbarButtonSize)))
                    SetRowsExpanded(true);
                GUI.enabled = true;

                EditorGUI.BeginChangeCheck();
                m_Table.flatView = !GUILayout.Toggle(!m_Table.flatView, Utility.GetIcon(Utility.IconType.Hierarchy, "Show/Hide Hierarchy"), EditorStyles.toolbarButton, GUILayout.Width(LayoutSize.ToolbarIconSize));
                if (EditorGUI.EndChangeCheck())
                {
                    MarkDirty();
                }

                GUI.enabled = !m_Table.flatView;

                Utility.ToolbarDropdownList(m_GroupDropdownItems, m_Table.groupPropertyIndex,
                    (data) =>
                    {
                        var groupPropertyIndex = (int)data;
                        if (groupPropertyIndex != m_Table.groupPropertyIndex)
                        {
                            SetRowsExpanded(false);

                            m_Table.groupPropertyIndex = groupPropertyIndex;
                            m_Table.Clear();
                            m_Table.AddIssues(m_Issues);
                            m_Table.Reload();
                        }
                    }, GUILayout.Width(toolbarButtonSize * 2));

                GUI.enabled = true;
            }

            if (m_Desc.showSeverityFilters)
            {
                EditorGUI.BeginChangeCheck();
                m_ShowInfo = GUILayout.Toggle(m_ShowInfo, Utility.GetIcon(Utility.IconType.Info, "Show info messages"), EditorStyles.toolbarButton, GUILayout.ExpandWidth(false));
                m_ShowWarn = GUILayout.Toggle(m_ShowWarn, Utility.GetIcon(Utility.IconType.Warning, "Show warnings"), EditorStyles.toolbarButton, GUILayout.ExpandWidth(false));
                m_ShowError = GUILayout.Toggle(m_ShowError, Utility.GetIcon(Utility.IconType.Error, "Show errors"), EditorStyles.toolbarButton, GUILayout.ExpandWidth(false));
                if (EditorGUI.EndChangeCheck())
                {
                    MarkDirty();
                }
            }
        }

        void DrawDataOptions()
        {
            if (m_Desc.onDrawToolbar != null)
                m_Desc.onDrawToolbar(m_ViewManager);

            if (Utility.ToolbarButtonWithDropdownList(Contents.ExportButton, k_ExportModeStrings,
                (data) =>
                {
                    var mode = (ExportMode)data;
                    switch (mode)
                    {
                        case ExportMode.All:
                            Export();
                            return;
                        case ExportMode.Filtered:
                            Export(Match);
                            return;
                        case ExportMode.Selected:
                            var selectedItems = table.GetSelectedItems();
                            Export(issue =>
                            {
                                return selectedItems.Any(item => item.Find(issue));
                            });
                            return;
                    }
                }, GUILayout.Width(toolbarButtonSize)))
            {
                Export();

                GUIUtility.ExitGUI();
            }
        }

        void DrawRecommendation(ProblemDescriptor[] selectedDescriptors)
        {
            EditorGUILayout.BeginVertical(GUI.skin.box, GUILayout.Width(LayoutSize.FoldoutWidth));
            m_ViewStates.recommendation = Utility.BoldFoldout(m_ViewStates.recommendation, Contents.RecommendationFoldout);
            if (m_ViewStates.recommendation)
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

            m_ViewStates.dependencies = Utility.BoldFoldout(m_ViewStates.dependencies, m_Desc.dependencyViewGuiContent != null ? m_Desc.dependencyViewGuiContent : Contents.Dependencies);
            if (m_ViewStates.dependencies)
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
                    var dependencies = selection.dependencies;

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

        public void SetSearch(string filter)
        {
            m_TextFilter.searchString = filter;
        }

        void SetRowsExpanded(bool expanded)
        {
            if (expanded)
            {
                var rows = m_Table.GetRows();

                m_Table.SetExpanded(rows.Select(r => r.id).ToList());
            }
            else
            {
                m_Table.SetExpanded(new List<int>());
            }
        }

        protected virtual void Export(Func<ProjectIssue, bool> predicate = null)
        {
            var path = EditorUtility.SaveFilePanel("Save to CSV file", UserPreferences.loadSavePath, string.Format("project-auditor-{0}.csv", m_Desc.category.ToString()).ToLower(),
                "csv");
            if (path.Length != 0)
            {
                using (var exporter = new CSVExporter(path, m_Layout))
                {
                    exporter.WriteHeader();

                    var matchingIssues = m_Issues.Where(issue => issue.descriptor == null || m_Config.GetAction(issue.descriptor, issue.GetContext()) !=
                        Rule.Severity.None && (predicate == null || predicate(issue)));
                    exporter.WriteIssues(matchingIssues.ToArray());
                }

                EditorUtility.RevealInFinder(path);

                if (m_ViewManager.onViewExported != null)
                    m_ViewManager.onViewExported();

                UserPreferences.loadSavePath = Path.GetDirectoryName(path);
            }
        }

        public virtual bool Match(ProjectIssue issue)
        {
            if (m_Desc.showSeverityFilters)
            {
                switch (issue.severity)
                {
                    case Rule.Severity.Info:
                        if (!m_ShowInfo)
                            return false;
                        break;
                    case Rule.Severity.Warning:
                        if (!m_ShowWarn)
                            return false;
                        break;
                    case Rule.Severity.Error:
                        if (!m_ShowError)
                            return false;
                        break;
                }
            }
            return m_BaseFilter.Match(issue) && m_TextFilter.Match(issue);
        }

        internal void OnEnable()
        {
            LoadSettings();
        }

        public virtual void LoadSettings()
        {
            var columns = m_Table.multiColumnHeader.state.columns;
            for (int i = 0; i < m_Layout.properties.Length; i++)
            {
                columns[i].width = EditorPrefs.GetFloat(GetPrefKey(k_ColumnSizeKey + i), columns[i].width);
            }

            var defaultGroupPropertyIndex = m_Layout.defaultGroupPropertyIndex;
            m_Table.flatView = EditorPrefs.GetBool(GetPrefKey(k_FlatModeKey), defaultGroupPropertyIndex == -1);
            m_Table.groupPropertyIndex = EditorPrefs.GetInt(GetPrefKey(k_GroupPropertyIndexKey), defaultGroupPropertyIndex);
            m_TextFilter.searchDependencies = EditorPrefs.GetBool(GetPrefKey(k_SearchDepsKey), false);
            m_TextFilter.ignoreCase = EditorPrefs.GetBool(GetPrefKey(k_SearchIgnoreCaseKey), true);
            m_TextFilter.searchString = EditorPrefs.GetString(GetPrefKey(k_SearchStringKey));
        }

        public virtual void SaveSettings()
        {
            var columns = m_Table.multiColumnHeader.state.columns;
            for (int i = 0; i < m_Layout.properties.Length; i++)
            {
                EditorPrefs.SetFloat(GetPrefKey(k_ColumnSizeKey + i), columns[i].width);
            }
            EditorPrefs.SetBool(GetPrefKey(k_FlatModeKey), m_Table.flatView);
            EditorPrefs.SetInt(GetPrefKey(k_GroupPropertyIndexKey), m_Table.groupPropertyIndex);
            EditorPrefs.SetBool(GetPrefKey(k_SearchDepsKey), m_TextFilter.searchDependencies);
            EditorPrefs.SetBool(GetPrefKey(k_SearchIgnoreCaseKey), m_TextFilter.ignoreCase);
            EditorPrefs.SetString(GetPrefKey(k_SearchStringKey), m_TextFilter.searchString);
        }

        string GetPrefKey(string key)
        {
            return $"{k_PrefKeyPrefix}.{m_Desc.name}.{key}";
        }

        public static void DrawToolbarButton(GUIContent guiContent, Action onClick)
        {
            if (GUILayout.Button(
                guiContent, EditorStyles.toolbarButton,
                GUILayout.Width(toolbarButtonSize)))
            {
                onClick();
            }
        }

        // pref keys
        const string k_PrefKeyPrefix = "ProjectAuditor.AnalysisView.";
        const string k_ColumnSizeKey = "ColumnSize";
        const string k_FlatModeKey = "FlatMode";
        const string k_GroupPropertyIndexKey = "GroupPropertyIndex";
        const string k_SearchDepsKey = "SearchDeps";
        const string k_SearchIgnoreCaseKey = "SearchIgnoreCase";
        const string k_SearchStringKey = "SearchString";

        // UI strings
        const string k_NoSelectionText = "<No selection>";
        const string k_AnalysisIsRequiredText = "<Missing Data: Please Analyze>";
        const string k_MultipleSelectionText = "<Multiple selection>";

        public static int toolbarButtonSize
        {
            get
            {
                return LayoutSize.ToolbarButtonSize;
            }
        }

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
            public static readonly int ToolbarButtonSize = 80;
            public static readonly int ToolbarIconSize = 40;
        }

        static class Contents
        {
            public static readonly GUIContent AnalyzeNow = new GUIContent("Analyze Now!");
            public static readonly GUIContent ExportButton = new GUIContent("Export", "Export current view to .csv file");
            public static readonly GUIContent ExpandAllButton = new GUIContent("Expand All");
            public static readonly GUIContent CollapseAllButton = new GUIContent("Collapse All");

            public static readonly GUIContent InfoFoldout = new GUIContent("Information");
            public static readonly GUIContent DetailsFoldout = new GUIContent("Details", "Issue Details");
            public static readonly GUIContent RecommendationFoldout =
                new GUIContent("Recommendation", "Recommendation on how to solve the issue");
            public static readonly GUIContent SearchStringLabel = new GUIContent("Search : ", "Text search options");
            public static readonly GUIContent TextSearchCaseSensitive = new GUIContent("Match Case", "Case-sensitive search");
            public static readonly GUIContent Dependencies = new GUIContent("Dependencies");
        }
    }
}
