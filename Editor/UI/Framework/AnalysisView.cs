using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.ProjectAuditor.Editor.Core;
using Unity.ProjectAuditor.Editor.Interfaces;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using UnityEngine.Profiling;

namespace Unity.ProjectAuditor.Editor.UI.Framework
{
    internal class AnalysisView : IIssueFilter
    {
        enum ExportMode
        {
            All = 0,
            Filtered = 1,
            Selected
        }

        protected Draw2D m_2D;
        protected bool m_Dirty = true;
        protected SeverityRules m_Rules;
        protected ViewStates m_ViewStates;
        protected ViewDescriptor m_Desc;
        protected IIssueFilter m_BaseFilter;
        protected List<ProjectIssue> m_Issues = new List<ProjectIssue>();
        protected IssueLayout m_Layout;
        protected IssueTable m_Table;
        protected TextFilter m_TextFilter;
        protected ViewManager m_ViewManager;

        DependencyView m_DependencyView;
        GUIContent m_HelpButtonContent;
        Utility.DropdownItem[] m_GroupDropdownItems;

        int m_SortPropertyIndex = -1;
        bool m_SortAscending = true;

        public ViewDescriptor Desc => m_Desc;

        public virtual string Description => $"A list of {m_Desc.displayName} found in the project.";

        public string DocumentationUrl => Documentation.GetPageUrl(new string(m_Desc.displayName.Where(char.IsLetterOrDigit).ToArray()));

        public int NumIssues => m_Issues.Count();

        public int NumFilteredIssues => m_Table.GetNumMatchingIssues();

        internal ViewManager ViewManager
        {
            get { return m_ViewManager; }
        }

        public AnalysisView(ViewManager viewManager)
        {
            m_2D = new Draw2D("Unlit/ProjectAuditor");

            m_ViewManager = viewManager;
        }

        public virtual void Create(ViewDescriptor descriptor, IssueLayout layout, SeverityRules rules, ViewStates viewStates, IIssueFilter filter)
        {
            m_Desc = descriptor;
            m_Rules = rules;
            m_ViewStates = viewStates;
            m_BaseFilter = filter;
            m_Layout = layout;

            if (layout.properties == null || layout.properties.Length == 0)
                return;

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

                var minWidth = 20;
                var width = 80;
                switch (property.type)
                {
                    case PropertyType.Description:
                        width = 300;
                        if (m_SortPropertyIndex == -1)
                            m_SortPropertyIndex = i;
                        break;
                    case PropertyType.Path:
                        width = 500;
                        break;
                    case PropertyType.LogLevel:
                    case PropertyType.Severity:
                        width = 24;
                        m_SortPropertyIndex = i;
                        break;
                }

                if (property.hidden)
                    minWidth = width = 0;

                columns[i] = new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent(property.name, layout.properties[i].longName),
                    width = width,
                    minWidth = minWidth,
                    autoResize = true
                };
            }

            var multiColumnHeader = new MultiColumnHeader(new MultiColumnHeaderState(columns));

            // set default sorting column (priority: Severity/logLevel, description or first column)
            multiColumnHeader.SetSorting(m_SortPropertyIndex != -1 ? m_SortPropertyIndex : 0, m_SortAscending);

            m_Table = new IssueTable(state,
                multiColumnHeader,
                m_Desc,
                layout,
                m_Rules,
                this);

            if (m_Desc.showDependencyView)
                m_DependencyView = new DependencyView(new TreeViewState(), m_Desc.onOpenIssue);

            if (m_TextFilter == null)
                m_TextFilter = new TextFilter(layout.properties);

            var helpButtonTooltip = string.Format("Open Reference for {0}", m_Desc.displayName);
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
            if (m_Table == null)
                return;

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

        // TODO: remove this method when not used anymore
        public bool IsDiagnostic()
        {
            return m_Layout.properties.Any(p => p.type == PropertyType.Severity);
        }

        public bool IsValid()
        {
            return m_Layout.properties.Length == 0 || m_Table != null;
        }

        public virtual void DrawFilters()
        {
        }

        public virtual void DrawContent(bool showDetails = false)
        {
            var selectedItems = m_Table.GetSelectedItems();
            var selectedIssues = selectedItems.Where(i => i.ProjectIssue != null).Select(i => i.ProjectIssue).ToArray();

            using (new EditorGUILayout.HorizontalScope(GUI.skin.box, GUILayout.ExpandHeight(true)))
            {
                DrawTable();

                if (showDetails)
                {
                    EditorGUILayout.BeginVertical(GUILayout.Width(LayoutSize.DetailsPanelWidth));

                    EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
                    EditorGUILayout.Space();
                    EditorGUILayout.EndHorizontal();

                    DrawDetails(selectedIssues);
                    EditorGUILayout.EndVertical();
                }
            }

            if (m_Desc.showDependencyView)
            {
                DrawDependencyView(selectedIssues);
            }
        }

        public void DrawTopPanel(bool drawDescription = true)
        {
            using (new EditorGUILayout.VerticalScope(GUI.skin.box, GUILayout.ExpandWidth(true)))
            {
                if (drawDescription)
                    EditorGUILayout.LabelField(Description);

                // Add a bit of space to improve readability
                EditorGUILayout.Space();

                if (!m_Desc.showInfoPanel)
                    return;

                m_ViewStates.info = Utility.BoldFoldout(m_ViewStates.info, Contents.InfoFoldout);
                if (m_ViewStates.info)
                {
                    EditorGUI.indentLevel++;

                    DrawInfo();

                    EditorGUI.indentLevel--;
                }
            }
        }

        protected virtual void DrawInfo()
        {
        }

        void DrawTable()
        {
            RefreshIfDirty();

            EditorGUILayout.BeginVertical();

            DrawToolbar();

            var r = EditorGUILayout.GetControlRect(GUILayout.ExpandHeight(true));

            Profiler.BeginSample("IssueTable.OnGUI");
            m_Table.OnGUI(r);
            Profiler.EndSample();

            EditorGUILayout.EndVertical();
        }

        public virtual void DrawSearch()
        {
            EditorGUILayout.BeginHorizontal();

            // note that we don't need to detect string changes (with EditorGUI.Begin/EndChangeCheck(), because the TreeViewController already triggers a BuildRows() when the text changes
            EditorGUILayout.LabelField(Contents.SearchStringLabel, GUILayout.Width(80));

            m_TextFilter.searchString = EditorGUILayout.DelayedTextField(m_TextFilter.searchString, GUILayout.Width(280));
            m_Table.searchString = m_TextFilter.searchString;

            EditorGUI.BeginChangeCheck();

            if (UserPreferences.DeveloperMode && m_Desc.showDependencyView)
            {
                // this is only available in developer mode because it is still too slow at the moment
                m_TextFilter.searchDependencies = EditorGUILayout.ToggleLeft("Dependencies (slow)",
                    m_TextFilter.searchDependencies, GUILayout.Width(160));
            }

            if (EditorGUI.EndChangeCheck())
                MarkDirty();

            GUILayout.FlexibleSpace();

            EditorGUILayout.EndHorizontal();
        }

        void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            DrawViewOptions();

            EditorGUILayout.Space();

            DrawDataOptions();

            EditorGUILayout.EndHorizontal();
        }

        public virtual void DrawDetails(ProjectIssue[] selectedIssues)
        {
            EditorGUILayout.BeginVertical();
            EditorGUILayout.EndVertical();
        }

        public virtual void DrawViewOptions()
        {
            if (m_ViewManager.OnAnalysisRequested != null)
                DrawToolbarButtonIcon(Contents.AnalyzeNowButton,  () => m_ViewManager.OnAnalysisRequested(m_Desc.category));

            m_Table.SetFontSize(m_ViewStates.fontSize);

            if (!m_Layout.hierarchy)
            {
                EditorGUI.BeginChangeCheck();
                m_Table.flatView = !GUILayout.Toggle(!m_Table.flatView, Contents.HierarchyButton, EditorStyles.toolbarButton, GUILayout.Width(LayoutSize.ToolbarIconSize));
                if (EditorGUI.EndChangeCheck())
                {
                    MarkDirty();
                }

                using (new EditorGUI.DisabledScope(m_Table.flatView))
                {
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
                        }, GUILayout.Width(ToolbarButtonSize * 2));

                    // collapse/expand buttons
                    DrawToolbarButton(Contents.CollapseAllButton,  () => SetRowsExpanded(false));
                    DrawToolbarButton(Contents.ExpandAllButton,  () => SetRowsExpanded(true));
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
                            var selectedItems = m_Table.GetSelectedItems();
                            Export(issue =>
                            {
                                return selectedItems.Any(item => item.Find(issue));
                            });
                            return;
                    }
                }, GUILayout.Width(ToolbarButtonSize)))
            {
                Export();

                GUIUtility.ExitGUI();
            }
        }

        void DrawDependencyView(ProjectIssue[] issues)
        {
            EditorGUILayout.BeginVertical(GUI.skin.box, GUILayout.Height(LayoutSize.DependencyViewHeight));

            m_ViewStates.dependencies = Utility.BoldFoldout(m_ViewStates.dependencies, m_Desc.dependencyViewGuiContent != null ? m_Desc.dependencyViewGuiContent : Contents.Dependencies);
            if (m_ViewStates.dependencies)
            {
                if (issues.Length == 0)
                {
                    EditorGUILayout.LabelField(k_NoSelectionText, SharedStyles.TextAreaWithDynamicSize, GUILayout.MaxHeight(LayoutSize.FoldoutMaxHeight));
                }
                else if (issues.Length > 1)
                {
                    EditorGUILayout.LabelField(k_MultipleSelectionText, SharedStyles.TextAreaWithDynamicSize, GUILayout.MaxHeight(LayoutSize.FoldoutMaxHeight));
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
                        EditorGUILayout.LabelField(k_AnalysisIsRequiredText, SharedStyles.TextAreaWithDynamicSize, GUILayout.MaxHeight(LayoutSize.FoldoutMaxHeight));
                    }
                }
            }

            EditorGUILayout.EndVertical();
        }

        public void SetSearch(string filter)
        {
            m_TextFilter.searchString = filter;
        }

        public ProjectIssue[] GetSelection()
        {
            var selectedItems = m_Table.GetSelectedItems();
            return selectedItems.Where(item => item.parent != null).Select(i => i.ProjectIssue).ToArray();
        }

        public void SetSelection(Func<ProjectIssue, bool> predicate)
        {
            RefreshIfDirty();

            // Expand all rows. This is a workaround for the fact that we can't select rows that are not visible.
            SetRowsExpanded(true);

            var rows = m_Table.GetRows();
            var selectedIDs = rows.Select(item => item as IssueTableItem).Where(i => i != null && i.ProjectIssue != null && predicate(i.ProjectIssue)).Select(i => i.id).ToList();

            m_Table.SetSelection(selectedIDs);
        }

        public void FrameSelection()
        {
            var selectedItems = m_Table.GetSelectedItems();
            if (selectedItems.Length > 0)
            {
                var firstItem = selectedItems[0];
                m_Table.FrameItem(firstItem.id);
            }
        }

        public void ClearSelection()
        {
            m_Table.SetSelection(new List<int>());
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
            var path = EditorUtility.SaveFilePanel("Save to CSV file", UserPreferences.LoadSavePath, string.Format("project-auditor-{0}.csv", m_Desc.category.ToString()).ToLower(),
                "csv");
            if (path.Length != 0)
            {
                using (var exporter = new CsvExporter(path, m_Layout))
                {
                    exporter.WriteHeader();

                    var matchingIssues = m_Issues.Where(issue => predicate == null || predicate(issue));
                    exporter.WriteIssues(matchingIssues.ToArray());
                }

                EditorUtility.RevealInFinder(path);

                if (m_ViewManager.OnViewExportCompleted != null)
                    m_ViewManager.OnViewExportCompleted();

                UserPreferences.LoadSavePath = Path.GetDirectoryName(path);
            }
        }

        public virtual bool Match(ProjectIssue issue)
        {
            return m_BaseFilter.Match(issue) && m_TextFilter.Match(issue);
        }

        internal void OnEnable()
        {
            LoadSettings();
        }

        public virtual void LoadSettings()
        {
            if (m_Table == null)
                return;

            var columns = m_Table.multiColumnHeader.state.columns;
            for (int i = 0; i < m_Layout.properties.Length; i++)
            {
                // when reloading we need to make sure visible columns have a width >= minWidth
                columns[i].width = m_Layout.properties[i].hidden ? 0 : Math.Max(columns[i].minWidth, EditorPrefs.GetFloat(GetPrefKey(k_ColumnSizeKey + i), columns[i].width));
            }

            var defaultGroupPropertyIndex = m_Layout.defaultGroupPropertyIndex;
            m_Table.flatView = EditorPrefs.GetBool(GetPrefKey(k_FlatModeKey), defaultGroupPropertyIndex == -1);
            m_Table.showIgnoredIssues = EditorPrefs.GetBool(GetPrefKey(k_ShowIgnoredIssuesKey), false);
            m_Table.groupPropertyIndex = EditorPrefs.GetInt(GetPrefKey(k_GroupPropertyIndexKey), defaultGroupPropertyIndex);
            m_SortPropertyIndex = EditorPrefs.GetInt(GetPrefKey(k_SortPropertyIndexKey), 0);
            m_SortAscending = EditorPrefs.GetBool(GetPrefKey(k_SortAscendingKey), true);
            m_Table.multiColumnHeader.SetSorting(m_SortPropertyIndex, m_SortAscending);

            m_TextFilter.searchDependencies = EditorPrefs.GetBool(GetPrefKey(k_SearchDepsKey), false);
            m_TextFilter.ignoreCase = EditorPrefs.GetBool(GetPrefKey(k_SearchIgnoreCaseKey), true);
            m_TextFilter.searchString = EditorPrefs.GetString(GetPrefKey(k_SearchStringKey));
        }

        public virtual void SaveSettings()
        {
            if (m_Table == null)
                return;

            var columns = m_Table.multiColumnHeader.state.columns;
            for (int i = 0; i < m_Layout.properties.Length; i++)
            {
                EditorPrefs.SetFloat(GetPrefKey(k_ColumnSizeKey + i), columns[i].width);
            }
            EditorPrefs.SetBool(GetPrefKey(k_FlatModeKey), m_Table.flatView);
            EditorPrefs.SetBool(GetPrefKey(k_ShowIgnoredIssuesKey), m_Table.showIgnoredIssues);
            EditorPrefs.SetInt(GetPrefKey(k_GroupPropertyIndexKey), m_Table.groupPropertyIndex);

            EditorPrefs.SetInt(GetPrefKey(k_SortPropertyIndexKey), m_SortPropertyIndex);
            EditorPrefs.SetBool(GetPrefKey(k_SortAscendingKey), m_SortAscending);

            EditorPrefs.SetBool(GetPrefKey(k_SearchDepsKey), m_TextFilter.searchDependencies);
            EditorPrefs.SetBool(GetPrefKey(k_SearchIgnoreCaseKey), m_TextFilter.ignoreCase);
            EditorPrefs.SetString(GetPrefKey(k_SearchStringKey), m_TextFilter.searchString);
        }

        string GetPrefKey(string key)
        {
            return $"{k_PrefKeyPrefix}.{m_Desc.displayName}.{key}";
        }

        public static void DrawActionButton(GUIContent guiContent, Action onClick)
        {
            if (GUILayout.Button(guiContent, GUILayout.Height(LayoutSize.ActionButtonHeight)))
            {
                onClick();
            }
        }

        public static void DrawToolbarButton(GUIContent guiContent, Action onClick)
        {
            if (GUILayout.Button(
                guiContent, EditorStyles.toolbarButton,
                GUILayout.Width(ToolbarButtonSize)))
            {
                onClick();
            }
        }

        public static void DrawToolbarLargeButton(GUIContent guiContent, Action onClick)
        {
            if (GUILayout.Button(
                guiContent, EditorStyles.toolbarButton,
                GUILayout.Width(LayoutSize.ToolbarLargeButtonSize)))
            {
                onClick();
            }
        }

        public static void DrawToolbarButtonIcon(GUIContent guiContent, Action onClick)
        {
            if (GUILayout.Button(
                guiContent, EditorStyles.toolbarButton,
                GUILayout.Width(LayoutSize.ToolbarIconSize)))
            {
                onClick();
            }
        }

        // pref keys
        const string k_PrefKeyPrefix = "ProjectAuditor.AnalysisView.";
        const string k_ColumnSizeKey = "ColumnSize";
        const string k_FlatModeKey = "FlatMode";
        const string k_ShowIgnoredIssuesKey = "ShowIgnoredIssues";
        const string k_GroupPropertyIndexKey = "GroupPropertyIndex";
        const string k_SortPropertyIndexKey = "SortPropertyIndex";
        const string k_SortAscendingKey = "SortAscending";
        const string k_SearchDepsKey = "SearchDeps";
        const string k_SearchIgnoreCaseKey = "SearchIgnoreCase";
        const string k_SearchStringKey = "SearchString";

        // UI strings
        protected const string k_NoSelectionText = "<No selection>";
        protected const string k_AnalysisIsRequiredText = "<Missing Data: Please Analyze>";
        protected const string k_MultipleSelectionText = "<Multiple selection>";

        public static int ToolbarButtonSize => LayoutSize.ToolbarButtonSize;
        public static int ToolbarIconSize => LayoutSize.ToolbarIconSize;

        static readonly string[] k_ExportModeStrings =
        {
            "All",
            "Filtered",
            "Selected"
        };

        protected static class LayoutSize
        {
            public static readonly int FoldoutWidth = 260;
            public static readonly int FoldoutMaxHeight = 220;
            public static readonly int DependencyViewHeight = 200;
            public static readonly int DetailsPanelWidth = 200;
            public static readonly int ToolbarButtonSize = 80;
            public static readonly int ToolbarLargeButtonSize = 120;
            public static readonly int ToolbarIconSize = 32;
            public static readonly int ActionButtonHeight = 30;
            public static readonly int CopyToClipboardButtonSize = 24;
        }

        static class Contents
        {
            public static readonly GUIContent AnalyzeNowButton = Utility.GetIcon(Utility.IconType.Refresh, "Analyze Now!");
            public static readonly GUIContent HierarchyButton = Utility.GetIcon(Utility.IconType.Hierarchy, "Show/Hide Hierarchy");

            public static readonly GUIContent ExportButton = new GUIContent("Export", "Export current view to .csv file");
            public static readonly GUIContent ExpandAllButton = new GUIContent("Expand All");
            public static readonly GUIContent CollapseAllButton = new GUIContent("Collapse All");

            public static readonly GUIContent InfoFoldout = new GUIContent("Information");
            public static readonly GUIContent SearchStringLabel = new GUIContent("Search : ", "Text search options");
            public static readonly GUIContent Dependencies = new GUIContent("Dependencies");
        }
    }
}
