using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace Unity.ProjectAuditor.Editor
{
    internal class IssueTable : TreeView
    {
        public enum Column
        {
            Description = 0,
            Priority,
            Area,
            Filename,
            Assembly,

            Count
        }

        private static readonly string PerfCriticalIconName = "console.warnicon";

        private readonly ProjectAuditorConfig m_Config;

        private readonly bool m_GroupByDescription;
        private readonly IIssuesFilter m_IssuesFilter;

        List<TreeViewItem> m_Rows = new List<TreeViewItem>(100);
        List<IssueTableItem> m_TreeViewItemGroups;
        List<IssueTableItem> m_TreeViewItemIssues;

        public IssueTable(TreeViewState state, MultiColumnHeader multicolumnHeader,
                          bool groupByDescription, ProjectAuditorConfig config, IIssuesFilter issuesFilter) : base(state,
                                                                                                                   multicolumnHeader)
        {
            m_Config = config;
            m_IssuesFilter = issuesFilter;
            m_GroupByDescription = groupByDescription;
            multicolumnHeader.sortingChanged += OnSortingChanged;
        }

        public void AddIssues(ProjectIssue[] issues)
        {
            var id = 1;

            if (m_GroupByDescription)
            {
                var descriptors = issues.Select(i => i.descriptor).Distinct();
                if (m_TreeViewItemGroups == null)
                {
                    m_TreeViewItemGroups = new List<IssueTableItem>(descriptors.Count());
                }

                foreach (var descriptor in descriptors)
                {
                    var groupItem = new IssueTableItem(id++, 0, descriptor);
                    m_TreeViewItemGroups.Add(groupItem);
                }
            }

            if (m_TreeViewItemIssues == null)
            {
                m_TreeViewItemIssues = new List<IssueTableItem>(issues.Length);
            }

            foreach (var issue in issues)
            {
                var depth = m_GroupByDescription ? 1 : 0;
                var item = new IssueTableItem(id++, depth, issue.name, issue.descriptor, issue);
                m_TreeViewItemIssues.Add(item);
            }
        }

        public void Reset()
        {
            if (m_TreeViewItemGroups != null)
                m_TreeViewItemGroups.Clear();
            if (m_TreeViewItemIssues != null)
                m_TreeViewItemIssues.Clear();
        }

        protected override TreeViewItem BuildRoot()
        {
            var idForHiddenRoot = -1;
            var depthForHiddenRoot = -1;
            var root = new TreeViewItem(idForHiddenRoot, depthForHiddenRoot, "root");

            return root;
        }

        protected override IList<TreeViewItem> BuildRows(TreeViewItem root)
        {
            m_Rows.Clear();

            var filteredItems = m_TreeViewItemIssues.Where(item => m_IssuesFilter.ShouldDisplay(item.ProjectIssue));
            if (!filteredItems.Any())
            {
                m_Rows.Add(new TreeViewItem(0, 0, "No issue found"));
                return m_Rows;
            }

            if (m_GroupByDescription)
            {
                var descriptors = filteredItems.Select(i => i.ProblemDescriptor).Distinct();
                foreach (var descriptor in descriptors)
                {
                    var group = m_TreeViewItemGroups.Find(g => g.ProblemDescriptor.Equals(descriptor));
                    m_Rows.Add(group);

                    var groupIsExpanded = state.expandedIDs.Contains(group.id);
                    var children = filteredItems.Where(item => item.ProblemDescriptor.Equals(descriptor));

                    group.displayName = string.Format("{0} ({1})", descriptor.description, children.Count());
                    if (group.children != null)
                        group.children.Clear();

                    foreach (var child in children)
                    {
                        if (groupIsExpanded)
                            m_Rows.Add(child);
                        group.AddChild(child);
                    }
                }
            }
            else
            {
                foreach (var item in filteredItems)
                {
                    m_Rows.Add(item);
                }
            }
            SortIfNeeded(m_Rows);

            return m_Rows;
        }

        protected override void RowGUI(RowGUIArgs args)
        {
            for (var i = 0; i < args.GetNumVisibleColumns(); ++i)
                CellGUI(args.GetCellRect(i), args.item, args.GetColumn(i), ref args);
        }

        private void CellGUI(Rect cellRect, TreeViewItem treeViewItem, int column, ref RowGUIArgs args)
        {
            // only indent first column
            if ((int)Column.Description == column)
            {
                var indent = GetContentIndent(treeViewItem) + extraSpaceBeforeIconAndLabel;
                cellRect.xMin += indent;
                CenterRectUsingSingleLineHeight(ref cellRect);
            }

            var item = treeViewItem as IssueTableItem;
            if (item == null)
            {
                if ((Column)column == Column.Description)
                    EditorGUI.LabelField(cellRect, new GUIContent(treeViewItem.displayName, treeViewItem.displayName));
                return;
            }

            var issue = item.ProjectIssue;
            var descriptor = item.ProblemDescriptor;
            var areaLongDescription = "This issue might have an impact on " + descriptor.area;

            var rule = m_Config.GetRule(descriptor, issue != null ? issue.callingMethod : string.Empty);
            if (rule == null && issue != null)
                // try to find non-specific rule
                rule = m_Config.GetRule(descriptor);
            if (rule != null && rule.action == Rule.Action.None) GUI.enabled = false;

            if (item.hasChildren)
                switch ((Column)column)
                {
                    case Column.Description:
                        EditorGUI.LabelField(cellRect, new GUIContent(item.displayName, item.displayName));
                        break;
                    case Column.Area:
                        EditorGUI.LabelField(cellRect, new GUIContent(descriptor.area, areaLongDescription));
                        break;
                }
            else
                switch ((Column)column)
                {
                    case Column.Priority:
                        if (issue.isPerfCriticalContext)
#if UNITY_2018_3_OR_NEWER
                            EditorGUI.LabelField(cellRect,
                            EditorGUIUtility.TrIconContent(PerfCriticalIconName, "Performance Critical Context"));
#else
                            EditorGUI.LabelField(cellRect, new GUIContent(EditorGUIUtility.FindTexture(PerfCriticalIconName), "Performance Critical Context"));
#endif
                        break;
                    case Column.Area:
                        if (!m_GroupByDescription)
                            EditorGUI.LabelField(cellRect, new GUIContent(descriptor.area, areaLongDescription));
                        break;
                    case Column.Description:
                        if (m_GroupByDescription)
                        {
                            EditorGUI.LabelField(cellRect,
                                new GUIContent(issue.description, issue.callingMethod));
                        }
                        else
                        {
                            var tooltip = descriptor.problem + " \n\n" + descriptor.solution;
                            EditorGUI.LabelField(cellRect, new GUIContent(issue.description, tooltip));
                        }

                        break;
                    case Column.Filename:
                        if (issue.filename != string.Empty)
                        {
                            var filename = string.Format("{0}:{1}", issue.filename, issue.line);

                            // display fullpath as tooltip
                            EditorGUI.LabelField(cellRect, new GUIContent(filename, issue.relativePath));
                        }

                        break;
                    case Column.Assembly:
                        if (issue.assembly != string.Empty)
                            EditorGUI.LabelField(cellRect, new GUIContent(issue.assembly, issue.assembly));

                        break;
                }

            if (rule != null && rule.action == Rule.Action.None) GUI.enabled = true;
        }

        protected override void DoubleClickedItem(int id)
        {
            var rows = FindRows(new[] {id});
            var item = rows.FirstOrDefault();
            if (item == null || item.hasChildren)
                return;

            var issueTableItem = item as IssueTableItem;
            if (issueTableItem == null)
                return;

            var issue = issueTableItem.ProjectIssue;
            if (issue.location != null && issue.location.IsValid())
            {
                if (File.Exists(issue.location.path))
                {
                    issue.location.Open();
                }
                else
                {
#if UNITY_2018_3_OR_NEWER
                    var window = SettingsService.OpenProjectSettings(issue.location.path);
                    window.Repaint();
#endif
                }
            }
        }

        public IssueTableItem[] GetSelectedItems()
        {
            var ids = GetSelection();
            if (ids.Count() > 0) return FindRows(ids).OfType<IssueTableItem>().ToArray();

            return new IssueTableItem[0];
        }

        private void OnSortingChanged(MultiColumnHeader _multiColumnHeader)
        {
            SortIfNeeded(GetRows());
        }

        private void SortIfNeeded(IList<TreeViewItem> rows)
        {
            if (rows.Count <= 1) return;

            if (multiColumnHeader.sortedColumnIndex == -1)
                return; // No column to sort for (just use the order the data are in)

            SortByMultipleColumns(rows);
            Repaint();
        }

        private void SortByMultipleColumns(IList<TreeViewItem> rows)
        {
            var sortedColumns = multiColumnHeader.state.sortedColumns;
            if (sortedColumns.Length == 0)
                return;

            var columnAscending = new bool[sortedColumns.Length];
            for (var i = 0; i < sortedColumns.Length; i++)
                columnAscending[i] = multiColumnHeader.IsSortedAscending(sortedColumns[i]);

            var root = new ItemTree(null);
            var stack = new Stack<ItemTree>();
            stack.Push(root);
            foreach (var row in rows)
            {
                var r = row as IssueTableItem;
                if (r == null)
                    continue;

                var activeParentDepth = stack.Peek().Depth;

                while (row.depth <= activeParentDepth)
                {
                    stack.Pop();
                    activeParentDepth = stack.Peek().Depth;
                }

                if (row.depth > activeParentDepth)
                {
                    var t = new ItemTree(r);
                    stack.Peek().AddChild(t);
                    stack.Push(t);
                }
            }

            root.Sort(sortedColumns, columnAscending);

            // convert back to rows
            var newRows = new List<TreeViewItem>(rows.Count);
            root.ToList(newRows);
            rows.Clear();
            foreach (var treeViewItem in newRows)
                rows.Add(treeViewItem);
        }

        internal class ItemTree
        {
            private readonly List<ItemTree> m_Children;
            private readonly IssueTableItem m_Item;

            public ItemTree(IssueTableItem i)
            {
                m_Item = i;
                m_Children = new List<ItemTree>();
            }

            public int Depth
            {
                get { return m_Item == null ? -1 : m_Item.depth; }
            }

            public void AddChild(ItemTree item)
            {
                m_Children.Add(item);
            }

            public void Sort(int[] columnSortOrder, bool[] isColumnAscending)
            {
                m_Children.Sort(delegate(ItemTree a, ItemTree b)
                {
                    var rtn = 0;
                    for (var i = 0; i < columnSortOrder.Length; i++)
                    {
                        ItemTree firstTree;
                        ItemTree secondTree;

                        if (isColumnAscending[i])
                        {
                            firstTree = a;
                            secondTree = b;
                        }
                        else
                        {
                            firstTree = b;
                            secondTree = a;
                        }

                        string firstString;
                        string secondString;

                        switch ((Column)columnSortOrder[i])
                        {
                            case Column.Description:
                                firstString = firstTree.m_Item.displayName;
                                secondString = secondTree.m_Item.displayName;
                                break;
                            case Column.Area:
                                firstString = firstTree.m_Item.ProblemDescriptor.area;
                                secondString = secondTree.m_Item.ProblemDescriptor.area;
                                break;
                            case Column.Filename:
                                firstString = firstTree.m_Item.ProjectIssue != null
                                    ? firstTree.m_Item.ProjectIssue.filename
                                    : string.Empty;
                                secondString = secondTree.m_Item.ProjectIssue != null
                                    ? secondTree.m_Item.ProjectIssue.filename
                                    : string.Empty;
                                break;
                            case Column.Assembly:
                                firstString = firstTree.m_Item.ProjectIssue != null
                                    ? firstTree.m_Item.ProjectIssue.assembly
                                    : string.Empty;
                                secondString = secondTree.m_Item.ProjectIssue != null
                                    ? secondTree.m_Item.ProjectIssue.assembly
                                    : string.Empty;
                                break;
                            default:
                                continue;
                        }

                        rtn = string.Compare(firstString, secondString, StringComparison.Ordinal);
                        if (rtn == 0)
                            continue;
                        return rtn;
                    }

                    return rtn;
                });

                foreach (var child in m_Children)
                    child.Sort(columnSortOrder, isColumnAscending);
            }

            public void ToList(List<TreeViewItem> list)
            {
                // TODO be good to optimise this, rarely used, so not required
                if (m_Item != null)
                    list.Add(m_Item);
                foreach (var child in m_Children)
                    child.ToList(list);
            }
        }
    }
}
