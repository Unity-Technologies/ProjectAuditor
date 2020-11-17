using System;
using System.Collections.Generic;
using System.Linq;
using Unity.ProjectAuditor.Editor.CodeAnalysis;
using Unity.ProjectAuditor.Editor.Utils;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using UnityEngine.Profiling;

namespace Unity.ProjectAuditor.Editor.UI
{
    struct ColumnStyle
    {
        public GUIContent Content;
        public int Width;
        public int MinWidth;
    }

    struct AnalysisViewDescriptor
    {
        public IssueCategory category;
        public string name;
        public bool groupByDescription;
        public bool descriptionWithIcon;
        public bool showAssemblySelection;
        public bool showCritical;
        public bool showDependencyView;
        public bool showRightPanels;
        public GUIContent dependencyViewGuiContent;
        public IssueTable.Column[] columnDescriptors;
        public ColumnStyle[] costumColumnStyles;
        public Action<Location> onDoubleClick;
        public Action<ProblemDescriptor> onOpenDescriptor;
        public Action<ProjectIssue, DependencyNode> onDrawDependencies;
        public ProjectAuditorAnalytics.UIButton analyticsEvent;
    }

    class AnalysisView
    {
        readonly ProjectAuditorConfig m_Config;
        readonly Preferences m_Preferences;
        readonly AnalysisViewDescriptor m_Desc;
        readonly IProjectIssueFilter m_Filter;

        DependencyView m_DependencyView;
        IssueTable m_Table;

        public AnalysisViewDescriptor desc
        {
            get { return m_Desc; }
        }

        public DependencyView dependencyView
        {
            get { return m_DependencyView; }
        }

        public IssueTable table
        {
            get { return m_Table; }
        }

        public AnalysisView(AnalysisViewDescriptor desc, ProjectAuditorConfig config, Preferences prefs, IProjectIssueFilter filter)
        {
            m_Desc = desc;
            m_Config = config;
            m_Preferences = prefs;
            m_Filter = filter;
            m_Table = null;
        }

        public void CreateTable(Preferences prefs)
        {
            if (m_Table != null)
                return;

            var state = new TreeViewState();
            var columns = new MultiColumnHeaderState.Column[m_Desc.columnDescriptors.Length];
            for (var i = 0; i < columns.Length; i++)
            {
                var columnEnum = m_Desc.columnDescriptors[i];

                ColumnStyle style;
                if (columnEnum < IssueTable.Column.Custom)
                    style = Styles.Columns[(int)columnEnum];
                else
                {
                    style = m_Desc.costumColumnStyles[columnEnum - IssueTable.Column.Custom];
                }

                columns[i] = new MultiColumnHeaderState.Column
                {
                    headerContent = style.Content,
                    width = style.Width,
                    minWidth = style.MinWidth,
                    autoResize = true
                };
            }

            m_Table = new IssueTable(state,
                new MultiColumnHeader(new MultiColumnHeaderState(columns)),
                m_Desc,
                m_Config,
                prefs,
                m_Filter);

            if (m_Desc.showDependencyView)
                m_DependencyView = new DependencyView(new TreeViewState(), m_Desc.onDoubleClick);
        }

        public void AddIssues(IEnumerable<ProjectIssue> issues)
        {
            m_Table.AddIssues(issues.Where(i => i.category == m_Desc.category).ToArray());
        }

        public void Refresh()
        {
            m_Table.Reload();
        }

        public void OnGUI()
        {
            ProblemDescriptor problemDescriptor = null;
            var selectedItems = m_Table.GetSelectedItems();
            var selectedDescriptors = selectedItems.Select(i => i.ProblemDescriptor);
            var selectedIssues = selectedItems.Select(i => i.ProjectIssue);
            // find out if all descriptors are the same
            var firstDescriptor = selectedDescriptors.FirstOrDefault();
            if (selectedDescriptors.Count() == selectedDescriptors.Count(d => d.id == firstDescriptor.id))
                problemDescriptor = firstDescriptor;

            ProjectIssue issue = null;
            if (selectedIssues.Count() == 1)
            {
                issue = selectedIssues.First();
            }

            EditorGUILayout.BeginHorizontal();

            DrawTable(selectedItems, selectedIssues.ToArray());

            if (m_Desc.showRightPanels)
            {
                EditorGUILayout.BeginVertical(GUILayout.Width(LayoutSize.FoldoutWidth));
                DrawFoldouts(problemDescriptor);
                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.EndHorizontal();

            if (m_Desc.showDependencyView)
            {
                DependencyNode dependencies = null;
                if (issue != null && issue.dependencies != null)
                {
                    if (issue.dependencies as CallTreeNode != null)
                        dependencies = issue.dependencies.GetChild(); // skip self
                    else
                        dependencies = issue.dependencies;
                }

                dependencyView.SetRoot(dependencies);

                DrawDependencyView(issue, dependencies);
            }
        }

        void DrawTable(IssueTableItem[] selectedItems, ProjectIssue[] selectedIssues)
        {
            EditorGUILayout.BeginVertical();

            var r = EditorGUILayout.GetControlRect(GUILayout.ExpandHeight(true));

            Profiler.BeginSample("IssueTable.OnGUI");
            m_Table.OnGUI(r);
            Profiler.EndSample();

            if (m_Desc.groupByDescription)
            {
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button(Styles.CollapseAllButton, GUILayout.ExpandWidth(true), GUILayout.Width(100)))
                    SetRowsExpanded(false);
                if (GUILayout.Button(Styles.ExpandAllButton, GUILayout.ExpandWidth(true), GUILayout.Width(100)))
                    SetRowsExpanded(true);
                EditorGUILayout.EndHorizontal();
            }

            var info = selectedIssues.Length + " / " + m_Table.GetNumMatchingIssues() + " issues";
            EditorGUILayout.LabelField(info, GUILayout.ExpandWidth(true), GUILayout.Width(200));

            EditorGUILayout.EndVertical();
        }

        bool BoldFoldout(bool toggle, GUIContent content)
        {
            var foldoutStyle = new GUIStyle(EditorStyles.foldout);
            foldoutStyle.fontStyle = FontStyle.Bold;
            return EditorGUILayout.Foldout(toggle, content, foldoutStyle);
        }

        void DrawFoldouts(ProblemDescriptor problemDescriptor)
        {
            DrawDetailsFoldout(problemDescriptor);
            DrawRecommendationFoldout(problemDescriptor);
        }

        void DrawDetailsFoldout(ProblemDescriptor problemDescriptor)
        {
            EditorGUILayout.BeginVertical(GUI.skin.box, GUILayout.Width(LayoutSize.FoldoutWidth));

            m_Preferences.details = BoldFoldout(m_Preferences.details, Styles.DetailsFoldout);
            if (m_Preferences.details)
            {
                if (problemDescriptor != null)
                {
                    EditorStyles.textField.wordWrap = true;
                    GUILayout.TextArea(problemDescriptor.problem, GUILayout.MaxHeight(LayoutSize.FoldoutMaxHeight));
                }
                else
                {
                    EditorGUILayout.LabelField(k_NoIssueSelectedText);
                }
            }

            EditorGUILayout.EndVertical();
        }

        void DrawRecommendationFoldout(ProblemDescriptor problemDescriptor)
        {
            EditorGUILayout.BeginVertical(GUI.skin.box, GUILayout.Width(LayoutSize.FoldoutWidth));

            m_Preferences.recommendation = BoldFoldout(m_Preferences.recommendation, Styles.RecommendationFoldout);
            if (m_Preferences.recommendation)
            {
                if (problemDescriptor != null)
                {
                    EditorStyles.textField.wordWrap = true;
                    GUILayout.TextArea(problemDescriptor.solution, GUILayout.MaxHeight(LayoutSize.FoldoutMaxHeight));
                }
                else
                {
                    EditorGUILayout.LabelField(k_NoIssueSelectedText);
                }
            }

            EditorGUILayout.EndVertical();
        }

        void DrawDependencyView(ProjectIssue issue, DependencyNode root)
        {
            EditorGUILayout.BeginVertical(GUI.skin.box, GUILayout.Height(LayoutSize.DependencyViewHeight));

            m_Preferences.dependencies = BoldFoldout(m_Preferences.dependencies, m_Desc.dependencyViewGuiContent);
            if (m_Preferences.dependencies)
            {
                if (root != null)
                {
                    var r = EditorGUILayout.GetControlRect(GUILayout.ExpandHeight(true));

                    dependencyView.OnGUI(r);
                }
                else if (issue != null)
                {
                    GUIStyle s = new GUIStyle(EditorStyles.textField);
                    s.normal.textColor = Color.yellow;
                    EditorGUILayout.LabelField(k_AnalysisIsRequiredText, s, GUILayout.MaxHeight(LayoutSize.FoldoutMaxHeight));
                }
                else
                {
                    EditorGUILayout.LabelField(k_NoIssueSelectedText);
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

        const string k_NoIssueSelectedText = "No issue selected";
        const string k_AnalysisIsRequiredText = "Missing Data: Please Analyze";

        static class LayoutSize
        {
            public static readonly int FoldoutWidth = 300;
            public static readonly int FoldoutMaxHeight = 220;
            public static readonly int DependencyViewHeight = 200;
        }

        static class Styles
        {
            public static readonly GUIContent ExpandAllButton = new GUIContent("Expand All", "");
            public static readonly GUIContent CollapseAllButton = new GUIContent("Collapse All", "");
            public static readonly GUIContent DetailsFoldout = new GUIContent("Details", "Issue Details");
            public static readonly GUIContent RecommendationFoldout =
                new GUIContent("Recommendation", "Recommendation on how to solve the issue");

            public static readonly ColumnStyle[] Columns =
            {
                new ColumnStyle
                {
                    Content = new GUIContent("Issue", "Issue description"),
                    Width = 300,
                    MinWidth = 100
                },
                new ColumnStyle
                {
                    Content = new GUIContent(" ! ", "Issue priority"),
                    Width = 22,
                    MinWidth = 22
                },
                new ColumnStyle
                {
                    Content = new GUIContent("Area", "The area the issue might have an impact on"),
                    Width = 60,
                    MinWidth = 50
                },
                new ColumnStyle
                {
                    Content = new GUIContent("Path", "Path and line number"),
                    Width = 700,
                    MinWidth = 100
                },
                new ColumnStyle
                {
                    Content = new GUIContent("Filename", "Managed Assembly name"),
                    Width = 180,
                    MinWidth = 100
                },
                new ColumnStyle
                {
                    Content = new GUIContent("File Type", "File extension"),
                    Width = 80,
                    MinWidth = 80
                }
            };
        }
    }
}
