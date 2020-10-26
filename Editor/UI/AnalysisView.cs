using System;
using System.Collections.Generic;
using System.Linq;
using Unity.ProjectAuditor.Editor.Utils;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using UnityEngine.Profiling;

namespace Unity.ProjectAuditor.Editor.UI
{
    internal struct ColumnStyle
    {
        public GUIContent Content;
        public int Width;
        public int MinWidth;
    }

    internal struct AnalysisViewDescriptor
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

    internal class AnalysisView
    {
        private readonly ProjectAuditorConfig m_Config;
        private readonly AnalysisViewDescriptor m_Desc;
        private readonly IProjectIssueFilter m_Filter;

        private DependencyView m_DependencyView;
        private IssueTable m_Table;

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

        public AnalysisView(AnalysisViewDescriptor desc, ProjectAuditorConfig config, IProjectIssueFilter filter)
        {
            m_Desc = desc;
            m_Config = config;
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

        public void OnGUI()
        {
            var r = EditorGUILayout.GetControlRect(GUILayout.ExpandHeight(true));

            Profiler.BeginSample("IssueTable.OnGUI");
            m_Table.OnGUI(r);
            Profiler.EndSample();
            var selectedItems = m_Table.GetSelectedItems();
            var selectedIssues = selectedItems.Select(i => i.ProjectIssue).ToArray();
            var info = selectedIssues.Length + " / " + m_Table.GetNumMatchingIssues() + " issues";

            EditorGUILayout.LabelField(info, GUILayout.ExpandWidth(true), GUILayout.Width(200));
        }

        private static class Styles
        {
            public static readonly ColumnStyle[] Columns =
            {
                new ColumnStyle
                {
                    Content = new GUIContent("Issue", "Issue description"),
                    Width = 300,
                    MinWidth = 100,
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
                    MinWidth = 100,
                },
                new ColumnStyle
                {
                    Content = new GUIContent("Filename", "Managed Assembly name"),
                    Width = 180,
                    MinWidth = 100,
                },
                new ColumnStyle
                {
                    Content = new GUIContent("File Type", "File extension"),
                    Width = 80,
                    MinWidth = 80,
                },
            };
        }
    }
}
