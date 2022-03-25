using System;
using System.Collections.Generic;
using System.Linq;
using Unity.ProjectAuditor.Editor.CodeAnalysis;
using Unity.ProjectAuditor.Editor.Utils;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using UnityEngine.Profiling;

namespace Unity.ProjectAuditor.Editor.UI.Framework
{
    class IssueTable : TreeView
    {
        static readonly int k_DefaultRowHeight = 18;
        static readonly int k_FirstId = 1;

        readonly ProjectAuditorConfig m_Config;
        readonly ViewDescriptor m_Desc;
        readonly IProjectIssueFilter m_Filter;
        readonly IssueLayout m_Layout;
        readonly List<TreeViewItem> m_Rows = new List<TreeViewItem>(100);

        List<IssueTableItem> m_TreeViewItemGroups = new List<IssueTableItem>();
        IssueTableItem[] m_TreeViewItemIssues;
        int m_NextId;
        int m_NumMatchingIssues;
        bool m_FlatView;

        public bool flatView
        {
            get { return m_FlatView; }
            set
            {
                if (m_Desc.groupByDescriptor)
                    m_FlatView = value;
            }
        }

        public IssueTable(TreeViewState state, MultiColumnHeader multicolumnHeader,
                          ViewDescriptor desc, IssueLayout layout, ProjectAuditorConfig config,
                          IProjectIssueFilter filter) : base(state,
                                                             multicolumnHeader)
        {
            m_Config = config;
            m_Filter = filter;
            m_Desc = desc;
            m_Layout = layout;
            m_FlatView = !desc.groupByDescriptor;
            multicolumnHeader.sortingChanged += OnSortingChanged;
            showAlternatingRowBackgrounds = true;

            Clear();
        }

        public void AddIssues(ProjectIssue[] issues)
        {
            // update groups, if applicable
            if (m_Desc.groupByDescriptor)
            {
                var descriptors = issues.Select(i => i.descriptor).Distinct().ToArray();
                foreach (var d in descriptors)
                {
                    // if necessary, create a group
                    if (m_TreeViewItemGroups.All(g => g.ProblemDescriptor.id != d.id))
                        m_TreeViewItemGroups.Add((new IssueTableItem(m_NextId++, 0, d)));
                }
            }

            var itemsList = new List<IssueTableItem>(issues.Length);
            if (m_TreeViewItemIssues != null)
                itemsList.AddRange(m_TreeViewItemIssues);
            foreach (var issue in issues)
            {
                var depth = issue.depth;
                if (m_Desc.groupByDescriptor)
                    depth++;
                var item = new IssueTableItem(m_NextId++, depth, issue.name, issue.descriptor, issue);
                itemsList.Add(item);
            }

            m_TreeViewItemIssues = itemsList.ToArray();
        }

        public void Clear()
        {
            m_NextId = k_FirstId;
            m_TreeViewItemGroups.Clear();
            m_TreeViewItemIssues = new IssueTableItem[] {};
        }

        protected override TreeViewItem BuildRoot()
        {
            var idForHiddenRoot = -1;
            var depthForHiddenRoot = -1;
            var root = new TreeViewItem(idForHiddenRoot, depthForHiddenRoot, "root");

            if (m_Desc.groupByDescriptor)
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
                m_Rows.Add(new TreeViewItem(0, 0, "No items"));
                return m_Rows;
            }

            foreach (var group in m_TreeViewItemGroups)
            {
                if (group.children != null)
                    group.children.Clear();
            }

            Profiler.BeginSample("IssueTable.BuildRows");
            if (!hasSearch && !m_FlatView)
            {
                var descriptors = filteredItems.Select(i => i.ProblemDescriptor).Distinct();
                foreach (var descriptor in descriptors)
                {
                    var group = m_TreeViewItemGroups.Find(g => g.ProblemDescriptor.Equals(descriptor));
                    m_Rows.Add(group);

                    var groupIsExpanded = state.expandedIDs.Contains(group.id);
                    var children = filteredItems.Where(item => item.ProblemDescriptor.Equals(descriptor));

                    group.displayName = string.Format("{0} ({1})", descriptor.description, children.Count());

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
                    if (m_Desc.groupByDescriptor)
                    {
                        var group = m_TreeViewItemGroups.Find(g => g.ProblemDescriptor.Equals(item.ProblemDescriptor));
                        group.AddChild(item);
                    }

                    m_Rows.Add(item);
                }
            }
            SortIfNeeded(m_Rows);

            Profiler.EndSample();

            return m_Rows;
        }

        protected override IList<int> GetAncestors(int id)
        {
            if (m_TreeViewItemIssues == null || m_TreeViewItemIssues.Length == 0)
                return new List<int>();
            return base.GetAncestors(id);
        }

        protected override IList<int> GetDescendantsThatHaveChildren(int id)
        {
            if (m_TreeViewItemIssues == null || m_TreeViewItemIssues.Length == 0)
                return new List<int>();
            return base.GetDescendantsThatHaveChildren(id);
        }

        public void SetFontSize(int fontSize)
        {
            rowHeight = k_DefaultRowHeight * fontSize / Preferences.k_MinFontSize;
        }

        protected override void RowGUI(RowGUIArgs args)
        {
            for (var i = 0; i < args.GetNumVisibleColumns(); ++i)
                CellGUI(args.GetCellRect(i), args.item, args.GetColumn(i), ref args);
        }

        void CellGUI(Rect cellRect, TreeViewItem treeViewItem, int columnIndex, ref RowGUIArgs args)
        {
            var property = m_Layout.properties[columnIndex];
            var columnType = property.type;
            var item = treeViewItem as IssueTableItem;

            // indent first column, if necessary
            if (columnIndex == 0 && !hasSearch && !m_FlatView)
            {
                if (item.IsGroup())
                {
                    var indent = GetContentIndent(treeViewItem) + extraSpaceBeforeIconAndLabel;
                    cellRect.xMin += indent;
                    CenterRectUsingSingleLineHeight(ref cellRect);
                }
            }
            else if (m_Layout.hierarchy && property.type == PropertyType.Description)
            {
                var indent = GetContentIndent(treeViewItem);
                cellRect.xMin += indent;
                CenterRectUsingSingleLineHeight(ref cellRect);
            }

            var labelStyle = SharedStyles.Label;
            if (item == null)
            {
                if (columnType == PropertyType.Description)
                    EditorGUI.LabelField(cellRect, new GUIContent(treeViewItem.displayName, treeViewItem.displayName), labelStyle);
                return;
            }

            var issue = item.ProjectIssue;
            var descriptor = item.ProblemDescriptor;

            var rule = m_Config.GetRule(descriptor, issue != null ? issue.GetCallingMethod() : string.Empty);
            if (rule == null && issue != null)
                rule = m_Config.GetRule(descriptor); // try to find non-specific rule
            if (rule != null && rule.severity == Rule.Severity.None)
                GUI.enabled = false;

            if (item.IsGroup())
            {
                if (columnIndex == 0)
                {
                    // use all available space to display description
                    cellRect.xMax = args.rowRect.xMax;
                    EditorGUI.LabelField(cellRect, Utility.GetTextWithSeverityIcon(item.GetDisplayName(), item.GetDisplayName(), descriptor.severity), labelStyle);
                }
            }
            else
            {
                var areaNames = descriptor.GetAreasSummary();
                var areaLongDescription = "Areas that this issue might have an impact on";
                switch (columnType)
                {
                    case PropertyType.CriticalContext:
                    {
                        if (issue.isPerfCriticalContext)
                            EditorGUI.LabelField(cellRect, Utility.GetSeverityIcon(Rule.Severity.Warning, "Potential hot-path"), labelStyle);
                    }
                    break;
                    case PropertyType.Severity:
                    {
                        var icon = Utility.GetSeverityIcon(issue.severity);
                        if (icon != null)
                        {
                            EditorGUI.LabelField(cellRect, icon, labelStyle);
                        }
                    }
                    break;

                    case PropertyType.Area:
                        EditorGUI.LabelField(cellRect, new GUIContent(areaNames, areaLongDescription), labelStyle);
                        break;

                    case PropertyType.Description:
                        GUIContent guiContent = null;
                        if (issue.location != null && m_Desc.descriptionWithIcon)
                        {
                            guiContent =
                                Utility.GetTextContentWithAssetIcon(item.GetDisplayName(), issue.location.Path);
                        }

                        if (guiContent == null)
                        {
                            guiContent = new GUIContent(item.GetDisplayName(), descriptor.problem != null ? descriptor.problem : item.GetDisplayName());
                        }
                        EditorGUI.LabelField(cellRect, guiContent, labelStyle);
                        break;

                    case PropertyType.Filename:
                        // display fullpath as tooltip
                        EditorGUI.LabelField(cellRect, new GUIContent(issue.GetProperty(PropertyType.Filename), issue.GetProperty(PropertyType.Path)), labelStyle);
                        break;

                    case PropertyType.Path:
                        EditorGUI.LabelField(cellRect, new GUIContent(issue.GetProperty(PropertyType.Path)), labelStyle);
                        break;

                    case PropertyType.FileType:
                        EditorGUI.LabelField(cellRect, new GUIContent(issue.GetProperty(PropertyType.FileType)), labelStyle);
                        break;

                    default:
                        var customProperty = issue.GetProperty(columnType);
                        if (customProperty != string.Empty)
                        {
                            bool boolValue;
                            ulong ulongValue;
                            if (property.format == PropertyFormat.Bool && bool.TryParse(customProperty, out boolValue))
                            {
                                EditorGUI.Toggle(cellRect, boolValue);
                            }
                            else if (property.format == PropertyFormat.Bytes && ulong.TryParse(customProperty, out ulongValue))
                            {
                                EditorGUI.LabelField(cellRect, Formatting.FormatSize(ulongValue));
                            }
                            else if (property.format == PropertyFormat.Time && ulong.TryParse(customProperty, out ulongValue))
                            {
                                EditorGUI.LabelField(cellRect, Formatting.FormatTime(ulongValue));
                            }
                            else
                                EditorGUI.LabelField(cellRect, new GUIContent(customProperty), labelStyle);
                        }

                        break;
                }
            }


            if (rule != null && rule.severity == Rule.Severity.None)
                GUI.enabled = true;

            ShowContextMenu(cellRect, item);
        }

        new void CenterRectUsingSingleLineHeight(ref Rect rect)
        {
            float singleLineHeight = rowHeight;
            if (rect.height > singleLineHeight)
            {
                rect.y += (rect.height - singleLineHeight) * 0.5f;
                rect.height = singleLineHeight;
            }
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
                if (m_Desc.onOpenManual != null)
                {
                    m_Desc.onOpenManual(tableItem.ProblemDescriptor);
                }
                return;
            }

            if (m_Desc.onOpenIssue == null)
                return;

            var issue = tableItem.ProjectIssue;
            if (issue.location != null && issue.location.IsValid())
            {
                m_Desc.onOpenIssue(issue.location);
            }
        }

        protected override void SearchChanged(string newSearch)
        {
            // auto-expand groups containing selected items
            foreach (var id in state.selectedIDs)
            {
                var item = m_TreeViewItemIssues.FirstOrDefault(issue => issue.id == id && issue.parent != null);
                if (item != null && !state.expandedIDs.Contains(item.parent.id))
                {
                    state.expandedIDs.Add(item.parent.id);
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
            if (ids.Count() > 0)
                return FindRows(ids).OfType<IssueTableItem>().ToArray();

            return new IssueTableItem[0];
        }

        void OnSortingChanged(MultiColumnHeader _multiColumnHeader)
        {
            if (m_Layout.hierarchy)
                return;

            SortIfNeeded(GetRows());
        }

        void ShowContextMenu(Rect cellRect, IssueTableItem item)
        {
            var current = Event.current;
            if (cellRect.Contains(current.mousePosition) && current.type == EventType.ContextClick)
            {
                var menu = new GenericMenu();

                menu.AddItem(Utility.CopyToClipboard, false, () => CopyToClipboard(item.GetDisplayName()));

                if (m_Desc.onOpenIssue != null && item.ProjectIssue != null && item.ProjectIssue.location != null)
                {
                    menu.AddItem(Utility.OpenIssue, false, () =>
                    {
                        m_Desc.onOpenIssue(item.ProjectIssue.location);
                    });
                }

                var desc = item.ProjectIssue != null && item.ProjectIssue.descriptor != null ? item.ProjectIssue.descriptor : null;
                if (m_Desc.onOpenManual != null && desc != null && desc.type.StartsWith("UnityEngine."))
                {
                    menu.AddItem(Utility.OpenScriptReference, false, () =>
                    {
                        m_Desc.onOpenManual(item.ProjectIssue.descriptor);
                    });
                }
                menu.ShowAsContext();

                current.Use();
            }
        }

        void CopyToClipboard(string text)
        {
            EditorGUIUtility.systemCopyBuffer = text;
        }

        void SortIfNeeded(IList<TreeViewItem> rows)
        {
            if (rows.Count <= 1) return;

            if (multiColumnHeader.sortedColumnIndex == -1)
                return; // No column to sort for (just use the order the data are in)

            SortByMultipleColumns(rows);
            Repaint();
        }

        void SortByMultipleColumns(IList<TreeViewItem> rows)
        {
            var sortedColumns = multiColumnHeader.state.sortedColumns;
            if (sortedColumns.Length == 0)
                return;

            var columnAscending = new bool[sortedColumns.Length];
            for (var i = 0; i < sortedColumns.Length; i++)
                columnAscending[i] = multiColumnHeader.IsSortedAscending(sortedColumns[i]);

            var root = new ItemTree(null, m_Layout);
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
                    var t = new ItemTree(r, m_Layout);
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
            readonly List<ItemTree> m_Children;
            readonly IssueTableItem m_Item;
            readonly IssueLayout m_Layout;

            public ItemTree(IssueTableItem i, IssueLayout layout)
            {
                m_Item = i;
                m_Children = new List<ItemTree>();
                m_Layout = layout;
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
                        var order = isColumnAscending[i] ? 1 : -1;
                        rtn = order * ProjectIssueExtensions.CompareTo(a.m_Item.ProjectIssue != null ? a.m_Item.ProjectIssue : null, b.m_Item.ProjectIssue != null ? b.m_Item.ProjectIssue : null, m_Layout.properties[columnSortOrder[i]].type);

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
