using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace Unity.ProjectAuditor.Editor
{
    internal class IssueTable : TreeView
    {
        internal class ItemTree
        {
            private IssueTableItem m_Item;
            private List<ItemTree> m_Children;

            public int Depth
            {
                get { return m_Item == null ? -1 : m_Item.depth; }
            }

            public ItemTree(IssueTableItem i)
            {
                m_Item = i;
                m_Children = new List<ItemTree>();
            }

            public void AddChild(ItemTree item)
            {
                m_Children.Add(item);
            }

            public void Sort(int[] columnSortOrder, bool[] isColumnAscending)
            {
                m_Children.Sort(delegate(ItemTree a, ItemTree b)
                {
                    int rtn = 0;
                    for (int i = 0; i < columnSortOrder.Length; i++)
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
                                firstString = firstTree.m_Item.problemDescriptor.area;
                                secondString = secondTree.m_Item.problemDescriptor.area; 
                                break;
                            case Column.Filename:
                                firstString = firstTree.m_Item.m_ProjectIssue != null ? firstTree.m_Item.m_ProjectIssue.filename : string.Empty;
                                secondString = secondTree.m_Item.m_ProjectIssue != null ? secondTree.m_Item.m_ProjectIssue.filename : string.Empty;
                                break;
                            case Column.Assembly:
                                firstString = firstTree.m_Item.m_ProjectIssue != null ? firstTree.m_Item.m_ProjectIssue.assembly : string.Empty;
                                secondString = secondTree.m_Item.m_ProjectIssue != null ? secondTree.m_Item.m_ProjectIssue.assembly : string.Empty;
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

                foreach (ItemTree child in m_Children)
                    child.Sort(columnSortOrder, isColumnAscending);
            }

            public void ToList(List<TreeViewItem> list)
            {
                // TODO be good to optimise this, rarely used, so not required
                if (m_Item != null)
                    list.Add(m_Item);
                foreach (ItemTree child in m_Children)
                    child.ToList(list);
            }
        }

        public enum Column
        {
            Description = 0,
            Area,
            Filename,
            Assembly,

            Count
        }

        private ProjectAuditor m_ProjectAuditor;
        private ProjectAuditorWindow m_ProjectAuditorWindow;
        ProjectIssue[] m_Issues;

        bool m_GroupByDescription;

        public IssueTable(TreeViewState state, MultiColumnHeader multicolumnHeader, ProjectIssue[] issues,
            bool groupByDescription, ProjectAuditor projectAuditor, ProjectAuditorWindow window) : base(state, multicolumnHeader)
        {
            m_ProjectAuditor = projectAuditor;
            m_ProjectAuditorWindow = window;
            m_Issues = issues;
            m_GroupByDescription = groupByDescription;
            multicolumnHeader.sortingChanged += OnSortingChanged;
            Reload();
        }

        protected override TreeViewItem BuildRoot()
        {
            // SteveM TODO - Documentation says that BuildRoot should ONLY build the root table item,
            // and all this logic should be moved to BuildRows()
            // https://docs.unity3d.com/ScriptReference/IMGUI.Controls.TreeView.BuildRows.html
            // This would involve implementing getNewSelectionOverride, GetAncestors() and GetDescendantsThatHaveChildren()
            // Which seems like a lot of extra complexity unless we're running into serious performance issues
            int index = 0;
            int idForHiddenRoot = -1;
            int depthForHiddenRoot = -1;
            var root = new TreeViewItem(idForHiddenRoot, depthForHiddenRoot, "root");

            if (m_GroupByDescription)
            {
                // grouped by problem definition
                HashSet<string> allGroupsSet = new HashSet<string>();
                foreach (var issue in m_Issues)
                {
                    if (!allGroupsSet.Contains(issue.descriptor.description))
                    {
                        if (m_ProjectAuditorWindow.ShouldDisplay(issue))
                        {
                            allGroupsSet.Add(issue.descriptor.description);
                        }
                    }
                }

                var allGroups = allGroupsSet.ToList();
                allGroups.Sort();

                foreach (var groupName in allGroups)
                {
                    var issues = m_Issues.Where(i => groupName.Equals(i.descriptor.description)).ToArray();

                    var displayName = string.Format("{0} ({1})", groupName, issues.Length);
                    var groupItem = new IssueTableItem(index++, 0, displayName, issues.FirstOrDefault().descriptor);
                    root.AddChild(groupItem);

                    foreach (var issue in issues)
                    {
                        if (m_ProjectAuditorWindow.ShouldDisplay(issue))
                        {
                            var item = new IssueTableItem(index++, 1, issue.name, issue.descriptor, issue);
                            groupItem.AddChild(item);
                        }
                    }
                }
            }
            else
            {
                // flat view
                foreach (var issue in m_Issues)
                {
                    if (m_ProjectAuditorWindow.ShouldDisplay(issue))
                    {
                        var item = new IssueTableItem(index++, 0, issue.descriptor.description, issue.descriptor, issue);
                        root.AddChild(item);
                    }
                }
            }

            if (!root.hasChildren)
                root.AddChild(new TreeViewItem(index++, 0, "No elements found"));

            return root;
        }

        protected override IList<TreeViewItem> BuildRows(TreeViewItem root)
        {
            var rows = base.BuildRows(root);
            SortIfNeeded(rows);
            return rows;
        }

        protected override void RowGUI(RowGUIArgs args)
        {
            for (int i = 0; i < args.GetNumVisibleColumns(); ++i)
            {
                CellGUI(args.GetCellRect(i), args.item, args.GetColumn(i), ref args);
            }
        }

        void CellGUI(Rect cellRect, TreeViewItem treeViewItem, int column, ref RowGUIArgs args)
        {
            // only indent first column
            if ((int) IssueTable.Column.Description == column)
            {
                var indent = GetContentIndent(treeViewItem) + extraSpaceBeforeIconAndLabel;
                cellRect.xMin += indent;
                CenterRectUsingSingleLineHeight(ref cellRect);
            }

            var item = (treeViewItem as IssueTableItem);
            if (item == null)
                return;

            var issue = item.m_ProjectIssue;
            var descriptor = item.problemDescriptor;
            var areaLongDescription = "This issue might have an impact on " + descriptor.area;

            var rule = m_ProjectAuditor.config.GetRule(descriptor, (issue != null) ? issue.callingMethod : string.Empty);
            if (rule == null && issue != null)
            {
                // try to find non-specific rule
                rule = m_ProjectAuditor.config.GetRule(descriptor);
            }
            if (rule != null && rule.action == Rule.Action.None)
            {
                GUI.enabled = false;
            }

            if (item.hasChildren)
            {
                switch ((Column) column)
                {
                    case Column.Description:
                        EditorGUI.LabelField(cellRect, new GUIContent(item.displayName, item.displayName));
                        break;
                    case Column.Area:
                        EditorGUI.LabelField(cellRect, new GUIContent(descriptor.area, areaLongDescription));
                        break;
                }
            }
            else
            {
                switch ((Column) column)
                {
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
                            string tooltip = descriptor.problem + " \n\n" + descriptor.solution;
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
                        {
                            EditorGUI.LabelField(cellRect, new GUIContent(issue.assembly, issue.assembly));
                        }

                        break;
                }
            }

            if (rule != null && rule.action == Rule.Action.None)
            {
                GUI.enabled = true;
            }
        }

        protected override void DoubleClickedItem(int id)
        {
            var rows = FindRows(new[] {id});
            var item = rows.FirstOrDefault();
            if (item != null && !item.hasChildren)
            {
                var issue = (item as IssueTableItem).m_ProjectIssue;
                if (issue.location != null && issue.location.IsValid())
                {
                    if (File.Exists(issue.location.path))
                        issue.location.Open();
                    else
                    {
                        var window = SettingsService.OpenProjectSettings(issue.location.path);
                        window.Repaint();                        
                    }
                }
            }
        }

        public IssueTableItem[] GetSelectedItems()
        {
            var ids = GetSelection();
            if (ids.Count() > 0)
            {
                return FindRows(ids).OfType<IssueTableItem>().ToArray();
            }

            return new IssueTableItem[0];
        }

        public int NumIssues(IssueCategory category)
        {
            return m_Issues.Count(i => i.category == category);
        }

        private void OnSortingChanged(MultiColumnHeader _multiColumnHeader)
        {
            SortIfNeeded(GetRows());
        }

        private void SortIfNeeded(IList<TreeViewItem> rows)
        {
            if (rows.Count <= 1)
            {
                return;
            }

            if (multiColumnHeader.sortedColumnIndex == -1)
            {
                return; // No column to sort for (just use the order the data are in)
            }

            SortByMultipleColumns(rows);
            Repaint();
        }

        private void SortByMultipleColumns(IList<TreeViewItem> rows)
        {
            int[] sortedColumns = multiColumnHeader.state.sortedColumns;
            if (sortedColumns.Length == 0)
                return;

            bool[] columnAscending = new bool[sortedColumns.Length];
            for (int i = 0; i < sortedColumns.Length; i++)
            {
                columnAscending[i] = multiColumnHeader.IsSortedAscending(sortedColumns[i]);
            }

            ItemTree root = new ItemTree(null);
            Stack<ItemTree> stack = new Stack<ItemTree>();
            stack.Push(root);
            foreach (TreeViewItem row in rows)
            {
                IssueTableItem r = row as IssueTableItem;
                if (r == null)
                    continue;

                int activeParentDepth = stack.Peek().Depth;

                while (row.depth <= activeParentDepth)
                {
                    stack.Pop();
                    activeParentDepth = stack.Peek().Depth;
                }

                if (row.depth > activeParentDepth)
                {
                    ItemTree t = new ItemTree(r);
                    stack.Peek().AddChild(t);
                    stack.Push(t);
                }
            }

            root.Sort(sortedColumns, columnAscending);

            // convert back to rows
            List<TreeViewItem> newRows = new List<TreeViewItem>(rows.Count);
            root.ToList(newRows);
            rows.Clear();
            foreach (TreeViewItem treeViewItem in newRows)
                rows.Add(treeViewItem);
        }
    }
}
