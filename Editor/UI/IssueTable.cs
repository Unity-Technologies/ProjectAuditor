using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using UnityEngine.Profiling;

namespace Unity.ProjectAuditor.Editor.UI
{
    internal class IssueTable : TreeView
    {
        public enum Column
        {
            Description = 0,
            Priority,
            Area,
            Path,
            Filename,
            FileType,
            Custom,

            Count
        }

        private static readonly string PerfCriticalIconName = "console.warnicon";

        private readonly ProjectAuditorConfig m_Config;
        private readonly Preferences m_Preferences;
        private readonly AnalysisViewDescriptor m_Desc;
        private readonly IProjectIssueFilter m_Filter;
        private readonly List<TreeViewItem> m_Rows = new List<TreeViewItem>(100);

        private List<IssueTableItem> m_TreeViewItemGroups;
        private IssueTableItem[] m_TreeViewItemIssues;
        private int m_NextId;
        private int m_NumMatchingIssues;

        public IssueTable(TreeViewState state, MultiColumnHeader multicolumnHeader,
                          AnalysisViewDescriptor desc, ProjectAuditorConfig config,
                          Preferences preferences,
                          IProjectIssueFilter filter) : base(state,
                                                             multicolumnHeader)
        {
            m_Config = config;
            m_Preferences = preferences;
            m_Filter = filter;
            m_Desc = desc;
            m_NextId = 1;
            multicolumnHeader.sortingChanged += OnSortingChanged;
        }

        public void AddIssues(ProjectIssue[] issues)
        {
            if (m_Desc.groupByDescription)
            {
                var descriptors = issues.Select(i => i.descriptor).Distinct();
                if (m_TreeViewItemGroups == null)
                {
                    m_TreeViewItemGroups = new List<IssueTableItem>(descriptors.Count());
                }

                foreach (var descriptor in descriptors)
                {
                    var groupItem = new IssueTableItem(m_NextId++, 0, descriptor);
                    m_TreeViewItemGroups.Add(groupItem);
                }
            }

            var itemsList = new List<IssueTableItem>(issues.Length);
            if (m_TreeViewItemIssues != null)
                itemsList.AddRange(m_TreeViewItemIssues);
            foreach (var issue in issues)
            {
                var depth = m_Desc.groupByDescription ? 1 : 0;
                var item = new IssueTableItem(m_NextId++, depth, issue.name, issue.descriptor, issue);
                itemsList.Add(item);
            }

            m_TreeViewItemIssues = itemsList.ToArray();
        }

        public void Reset()
        {
            m_NextId = 1;
            if (m_TreeViewItemGroups != null)
                m_TreeViewItemGroups.Clear();
            m_TreeViewItemIssues = null;
        }

        protected override TreeViewItem BuildRoot()
        {
            var idForHiddenRoot = -1;
            var depthForHiddenRoot = -1;
            var root = new TreeViewItem(idForHiddenRoot, depthForHiddenRoot, "root");

            if (m_Desc.groupByDescription)
            {
                foreach (var item in m_TreeViewItemGroups)
                {
                    root.AddChild(item);
                }
            }
            else
            {
                foreach (var item in m_TreeViewItemIssues)
                {
                    root.AddChild(item);
                }
            }

            return root;
        }

        protected override IList<TreeViewItem> BuildRows(TreeViewItem root)
        {
            m_Rows.Clear();

            // find all issues matching the filters and make an array out of them
            Profiler.BeginSample("IssueTable.Match");
            var filteredItems = m_TreeViewItemIssues.Where(item =>
            {
                return m_Filter.Match(item.ProjectIssue);
            }).ToArray();

            Profiler.EndSample();

            m_NumMatchingIssues = filteredItems.Length;
            if (m_NumMatchingIssues == 0)
            {
                m_Rows.Add(new TreeViewItem(0, 0, "No issue found"));
                return m_Rows;
            }

            Profiler.BeginSample("IssueTable.BuildRows");
            if (m_Desc.groupByDescription && !this.hasSearch)
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

            Profiler.EndSample();

            return m_Rows;
        }

        protected override void RowGUI(RowGUIArgs args)
        {
            for (var i = 0; i < args.GetNumVisibleColumns(); ++i)
                CellGUI(args.GetCellRect(i), args.item, args.GetColumn(i), ref args);
        }

        private void CellGUI(Rect cellRect, TreeViewItem treeViewItem, int columnIndex, ref RowGUIArgs args)
        {
            var column = m_Desc.columnDescriptors[columnIndex];

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
                if (column == Column.Description)
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

            if (item.IsGroup())
                switch (column)
                {
                    case Column.Description:
                        EditorGUI.LabelField(cellRect, new GUIContent(item.GetDisplayName(), item.GetDisplayName()));
                        break;
                    case Column.Area:
                        EditorGUI.LabelField(cellRect, new GUIContent(descriptor.area, areaLongDescription));
                        break;
                }
            else
                switch (column)
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
                        if (!m_Desc.groupByDescription)
                            EditorGUI.LabelField(cellRect, new GUIContent(descriptor.area, areaLongDescription));
                        break;
                    case Column.Description:
                        if (m_Desc.groupByDescription)
                        {
                            var text = item.GetDisplayName();
                            var tooltip = issue.callingMethod;
                            var guiContent = new GUIContent(text, tooltip);

#if UNITY_2018_3_OR_NEWER
                            if (m_Desc.descriptionWithIcon)
                            {
                                var icon = AssetDatabase.GetCachedIcon(issue.location.Path);
                                guiContent = EditorGUIUtility.TrTextContentWithIcon(text, tooltip, icon);
                            }
#endif

                            EditorGUI.LabelField(cellRect, guiContent);
                        }
                        else
                        {
                            var tooltip = descriptor.problem + " \n\n" + descriptor.solution;
                            EditorGUI.LabelField(cellRect, new GUIContent(item.GetDisplayName(), tooltip));
                        }

                        break;
                    case Column.Filename:
                        if (issue.filename != string.Empty)
                        {
                            var filename = string.Format("{0}", issue.filename);
                            if (issue.category == IssueCategory.Code)
                                filename += string.Format(":{0}", issue.line);

                            // display fullpath as tooltip
                            EditorGUI.LabelField(cellRect, new GUIContent(filename, issue.relativePath));
                        }
                        break;

                    case Column.Path:
                        if (issue.location != null)
                        {
                            var path = string.Format("{0}", issue.location.Path);
                            if (issue.category == IssueCategory.Code)
                                path += string.Format(":{0}", issue.line);

                            EditorGUI.LabelField(cellRect, new GUIContent(path, path));
                        }
                        break;

                    case Column.FileType:
                        if (issue.location.Path != string.Empty)
                        {
                            var ext = issue.location.Extension;
                            if (issue.category == IssueCategory.Assets && ext.StartsWith("."))
                                ext = ext.Substring(1);
                            EditorGUI.LabelField(cellRect, new GUIContent(ext, ext));
                        }

                        break;
                    default:
                        var propertyIndex = column - Column.Custom;
                        var property = issue.GetCustomProperty(propertyIndex);
                        if (property != string.Empty)
                            EditorGUI.LabelField(cellRect, new GUIContent(property, property));
                        break;
                }

            if (rule != null && rule.action == Rule.Action.None) GUI.enabled = true;
        }

        protected override void DoubleClickedItem(int id)
        {
            var rows = FindRows(new[] {id});
            var item = rows.FirstOrDefault();

            if (item == null)
                return;

            var tableItem = item as IssueTableItem;

            if (tableItem == null)
                return;
            if (item.hasChildren)
            {
                if (m_Desc.onOpenDescriptor != null)
                {
                    m_Desc.onOpenDescriptor(tableItem.ProblemDescriptor);
                }
                return;
            }

            var issue = tableItem.ProjectIssue;
            if (issue.location != null && issue.location.IsValid())
            {
                m_Desc.onDoubleClick(issue.location);
            }
        }

        protected override void SearchChanged(string newSearch)
        {
            // auto-expand groups containing selected items
            foreach (var id in this.state.selectedIDs)
            {
                var item = m_TreeViewItemIssues.FirstOrDefault(issue => issue.id == id && issue.parent != null);
                if (item != null && !this.state.expandedIDs.Contains(item.parent.id))
                {
                    this.state.expandedIDs.Add(item.parent.id);
                }
            }
        }

        public int GetNumMatchingIssues()
        {
            return m_NumMatchingIssues;
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

            var root = new ItemTree(null, m_Desc);
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
                    var t = new ItemTree(r, m_Desc);
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
            private readonly AnalysisViewDescriptor m_ViewDescriptor;

            public ItemTree(IssueTableItem i, AnalysisViewDescriptor viewDescriptor)
            {
                m_Item = i;
                m_Children = new List<ItemTree>();
                m_ViewDescriptor = viewDescriptor;
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
                        IssueTableItem firstItem;
                        IssueTableItem secondItem;

                        if (isColumnAscending[i])
                        {
                            firstItem = a.m_Item;
                            secondItem = b.m_Item;
                        }
                        else
                        {
                            firstItem = b.m_Item;
                            secondItem = a.m_Item;
                        }

                        string firstString;
                        string secondString;
                        var columnEnum = m_ViewDescriptor.columnDescriptors[columnSortOrder[i]];
                        switch (columnEnum)
                        {
                            case Column.Description:
                                firstString = firstItem.GetDisplayName();
                                secondString = secondItem.GetDisplayName();
                                break;
                            case Column.Area:
                                firstString = firstItem.ProblemDescriptor.area;
                                secondString = secondItem.ProblemDescriptor.area;
                                break;
                            case Column.Filename:
                                firstString = firstItem.ProjectIssue != null
                                    ? firstItem.ProjectIssue.filename
                                    : string.Empty;
                                secondString = secondItem.ProjectIssue != null
                                    ? secondItem.ProjectIssue.filename
                                    : string.Empty;
                                break;
                            case Column.Path:
                                firstString = firstItem.ProjectIssue != null
                                    ? firstItem.ProjectIssue.location.Path
                                    : string.Empty;
                                secondString = secondItem.ProjectIssue != null
                                    ? secondItem.ProjectIssue.location.Path
                                    : string.Empty;
                                break;
                            case Column.FileType:
                                firstString = firstItem.ProjectIssue != null ? firstItem.ProjectIssue.location.Extension : string.Empty;
                                secondString = secondItem.ProjectIssue != null ? secondItem.ProjectIssue.location.Extension : string.Empty;
                                break;
                            case Column.Priority:
                                firstString = firstItem.ProjectIssue != null ? firstItem.ProjectIssue.isPerfCriticalContext.ToString() : string.Empty;
                                secondString = secondItem.ProjectIssue != null ? secondItem.ProjectIssue.isPerfCriticalContext.ToString() : string.Empty;
                                break;
                            default:
                                var propertyIndex = columnEnum - Column.Custom;
                                firstString = firstItem.ProjectIssue != null
                                    ? firstItem.ProjectIssue.GetCustomProperty(propertyIndex)
                                    : string.Empty;
                                secondString = secondItem.ProjectIssue != null
                                    ? secondItem.ProjectIssue.GetCustomProperty(propertyIndex)
                                    : string.Empty;
                                break;
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
