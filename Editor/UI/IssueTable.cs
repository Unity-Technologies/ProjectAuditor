using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace Unity.ProjectAuditor.Editor
{
    class IssueTable : TreeView
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
                                firstString = firstTree.m_Item.m_ProjectIssue?.filename;
                                secondString = secondTree.m_Item.m_ProjectIssue?.filename;
                                break;
                            case Column.Assembly:
                                firstString = firstTree.m_Item.m_ProjectIssue?.assembly;
                                secondString = secondTree.m_Item.m_ProjectIssue?.assembly;
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
        ProjectIssue[] m_Issues;

        bool m_GroupByDescription;

        public IssueTable(TreeViewState state, MultiColumnHeader multicolumnHeader, ProjectIssue[] issues,
            bool groupByDescription, ProjectAuditor projectAuditor) : base(state, multicolumnHeader)
        {
            m_ProjectAuditor = projectAuditor;
            m_Issues = issues;
            m_GroupByDescription = groupByDescription;
            multicolumnHeader.sortingChanged += OnSortingChanged;
            Reload();
        }

        protected override TreeViewItem BuildRoot()
        {
            int index = 0;
            int idForhiddenRoot = -1;
            int depthForHiddenRoot = -1;
            var root = new TreeViewItem(idForhiddenRoot, depthForHiddenRoot, "root");

            if (m_GroupByDescription)
            {
                // grouped by problem definition
                HashSet<string> allGroupsSet = new HashSet<string>();
                foreach (var issue in m_Issues)
                {
                    if (!allGroupsSet.Contains(issue.descriptor.description))
                    {
                        allGroupsSet.Add(issue.descriptor.description);
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
                        var item = new IssueTableItem(index++, 1, issue.callingMethodName, issue.descriptor, issue);
                        groupItem.AddChild(item);
                    }
                }
            }
            else
            {
                // flat view
                foreach (var issue in m_Issues)
                {
                    var item = new IssueTableItem(index++, 0, issue.descriptor.description, issue.descriptor, issue);
                    root.AddChild(item);
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

            var rule = m_ProjectAuditor.config.GetRule(descriptor, issue?.callingMethodName);
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
                                new GUIContent(issue.callingMethodName, issue.callingMethod));
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
                var path = issue.relativePath;
                if (!string.IsNullOrEmpty(path))
                {
                    if ((path.StartsWith("Library/PackageCache") || path.StartsWith("Packages/") && path.Contains("@")))
                    {
                        // strip version from package path
                        var version = path.Substring(path.IndexOf("@"));
                        version = version.Substring(0, version.IndexOf("/"));
                        path = path.Replace(version, "").Replace("Library/PackageCache", "Packages");
                    }

                    var obj = AssetDatabase.LoadAssetAtPath<TextAsset>(path);
                    AssetDatabase.OpenAsset(obj, issue.line);
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
            return m_Issues.Where(i => i.category == category).Count();
        }

        void OnSortingChanged(MultiColumnHeader _multiColumnHeader)
        {
            SortIfNeeded(GetRows());
        }

        void SortIfNeeded(IList<TreeViewItem> rows)
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

        void SortByMultipleColumns(IList<TreeViewItem> rows)
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
