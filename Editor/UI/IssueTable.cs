using System;
using System.Collections.Generic;
using System.Linq;
using Packages.Editor.Utils;
using Unity.ProjectAuditor.Editor.CodeAnalysis;
using Unity.ProjectAuditor.Editor.Utils;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using UnityEngine.Profiling;

namespace Unity.ProjectAuditor.Editor.UI
{
    class IssueTable : TreeView
    {
        static readonly int k_FirstId = 1;

        static GUIStyle s_LabelStyle;

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
        int m_FontSize;

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
            m_NextId = k_FirstId;
            m_FontSize = Preferences.k_MinFontSize;
            multicolumnHeader.sortingChanged += OnSortingChanged;
        }

        public void AddIssues(ProjectIssue[] issues)
        {
            if (m_Desc.groupByDescriptor)
            {
                var descriptors = issues.Select(i => i.descriptor).Distinct().ToArray();
                var itemGroups = descriptors.Select(d => new IssueTableItem(m_NextId++, 0, d)).ToArray();
                m_TreeViewItemGroups.AddRange(itemGroups);
            }

            var itemsList = new List<IssueTableItem>(issues.Length);
            if (m_TreeViewItemIssues != null)
                itemsList.AddRange(m_TreeViewItemIssues);
            foreach (var issue in issues)
            {
                var depth = m_Desc.groupByDescriptor ? 1 : 0;
                var item = new IssueTableItem(m_NextId++, depth, issue.name, issue.descriptor, issue);
                itemsList.Add(item);
            }

            m_TreeViewItemIssues = itemsList.ToArray();
        }

        public void Clear()
        {
            m_NextId = k_FirstId;
            m_TreeViewItemGroups.Clear();
            m_TreeViewItemIssues = new IssueTableItem[] { };
        }

        public void SetFlatView(bool value)
        {
            m_FlatView = value;
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

            Profiler.BeginSample("IssueTable.BuildRows");
            if (m_Desc.groupByDescriptor && !hasSearch && !m_FlatView)
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
            const int k_DefaultRowHeight = 18;

            m_FontSize = fontSize;
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

            // indent first column, if necessary
            if (m_Desc.groupByDescriptor && columnIndex == 0)
            {
                var indent = GetContentIndent(treeViewItem) + extraSpaceBeforeIconAndLabel;
                cellRect.xMin += indent;
                CenterRectUsingSingleLineHeight(ref cellRect);
            }

            if (s_LabelStyle == null)
                s_LabelStyle = new GUIStyle(EditorStyles.label);
            s_LabelStyle.fontSize = m_FontSize;

            var item = treeViewItem as IssueTableItem;
            if (item == null)
            {
                if (columnType == PropertyType.Description)
                    EditorGUI.LabelField(cellRect, new GUIContent(treeViewItem.displayName, treeViewItem.displayName), s_LabelStyle);
                return;
            }

            var issue = item.ProjectIssue;
            var descriptor = item.ProblemDescriptor;
            var areaName = descriptor.area.Replace('|', ',');
            var areaLongDescription = "This issue might have an impact on " + descriptor.area;

            var rule = m_Config.GetRule(descriptor, issue != null ? issue.GetCallingMethod() : string.Empty);
            if (rule == null && issue != null)
                rule = m_Config.GetRule(descriptor); // try to find non-specific rule
            if (rule != null && rule.severity == Rule.Severity.None)
                GUI.enabled = false;

            if (item.IsGroup())
            {
                if (columnIndex == 0)
                {
                    switch (descriptor.severity)
                    {
                        case Rule.Severity.Warning:
                            EditorGUI.LabelField(cellRect, EditorGUIUtility.TrTextContentWithIcon(item.GetDisplayName(), item.GetDisplayName(), "console.warnicon"), s_LabelStyle);
                            break;
                        case Rule.Severity.Error:
                            EditorGUI.LabelField(cellRect, EditorGUIUtility.TrTextContentWithIcon(item.GetDisplayName(), item.GetDisplayName(), "console.erroricon"), s_LabelStyle);
                            break;
                        default:
                            EditorGUI.LabelField(cellRect, new GUIContent(item.GetDisplayName(), item.GetDisplayName()), s_LabelStyle);
                            break;
                    }
                }
                else if (columnType == PropertyType.Area)
                    EditorGUI.LabelField(cellRect, new GUIContent(areaName, areaLongDescription), s_LabelStyle);
            }
            else
                switch (columnType)
                {
                    case PropertyType.CriticalContext:
                    {
                        if (issue.isPerfCriticalContext)
                            EditorGUI.LabelField(cellRect, Utility.WarnIcon, s_LabelStyle);
                    }
                    break;
                    case PropertyType.Severity:
                    {
                        GUIContent icon = null;
                        switch (issue.descriptor.severity)
                        {
                            case Rule.Severity.Info:
                                icon = Utility.InfoIcon;
                                break;
                            case Rule.Severity.Warning:
                                icon = Utility.WarnIcon;
                                break;
                            case Rule.Severity.Error:
                                icon = Utility.ErrorIcon;
                                break;
                        }
                        if (icon != null)
                        {
                            EditorGUI.LabelField(cellRect, icon, s_LabelStyle);
                        }
                    }
                    break;

                    case PropertyType.Area:
                        if (!m_Desc.groupByDescriptor)
                            EditorGUI.LabelField(cellRect, new GUIContent(areaName, areaLongDescription), s_LabelStyle);
                        break;

                    case PropertyType.Description:
                        GUIContent guiContent = null;
                        if (issue.location != null && m_Desc.descriptionWithIcon)
                        {
#if UNITY_2018_3_OR_NEWER
                            var icon = AssetDatabase.GetCachedIcon(issue.location.Path);
                            guiContent = EditorGUIUtility.TrTextContentWithIcon(item.GetDisplayName(), issue.location.Path, icon);
#else
                            guiContent = new GUIContent(item.GetDisplayName(), issue.location.Path);
#endif
                        }

                        if (guiContent == null)
                        {
                            guiContent = new GUIContent(item.GetDisplayName(), descriptor.problem);
                        }
                        EditorGUI.LabelField(cellRect, guiContent, s_LabelStyle);
                        break;

                    case PropertyType.Filename:
                        // display fullpath as tooltip
                        EditorGUI.LabelField(cellRect, new GUIContent(issue.GetProperty(PropertyType.Filename), issue.GetProperty(PropertyType.Path)), s_LabelStyle);
                        break;

                    case PropertyType.Path:
                        EditorGUI.LabelField(cellRect, new GUIContent(issue.GetProperty(PropertyType.Path)), s_LabelStyle);
                        break;

                    case PropertyType.FileType:
                        EditorGUI.LabelField(cellRect, new GUIContent(issue.GetProperty(PropertyType.FileType)), s_LabelStyle);
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
                            else
                                EditorGUI.LabelField(cellRect, new GUIContent(customProperty), s_LabelStyle);
                        }

                        break;
                }

            if (rule != null && rule.severity == Rule.Severity.None)
                GUI.enabled = true;
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
            SortIfNeeded(GetRows());
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

                        var firstString = String.Empty;
                        var secondString = String.Empty;

                        var property = m_Layout.properties[columnSortOrder[i]];
                        switch (property.type)
                        {
                            case PropertyType.Description:
                                firstString = firstItem.GetDisplayName();
                                secondString = secondItem.GetDisplayName();
                                break;
                            case PropertyType.Area:
                                firstString = firstItem.ProblemDescriptor.area;
                                secondString = secondItem.ProblemDescriptor.area;
                                break;
                            case PropertyType.Filename:
                                firstString = firstItem.ProjectIssue != null
                                    ? firstItem.ProjectIssue.filename
                                    : string.Empty;
                                secondString = secondItem.ProjectIssue != null
                                    ? secondItem.ProjectIssue.filename
                                    : string.Empty;
                                break;
                            case PropertyType.Path:
                                firstString = firstItem.ProjectIssue != null
                                    ? firstItem.ProjectIssue.location.Path
                                    : string.Empty;
                                secondString = secondItem.ProjectIssue != null
                                    ? secondItem.ProjectIssue.location.Path
                                    : string.Empty;
                                break;
                            case PropertyType.FileType:
                                firstString = firstItem.ProjectIssue != null ? firstItem.ProjectIssue.location.Extension : string.Empty;
                                secondString = secondItem.ProjectIssue != null ? secondItem.ProjectIssue.location.Extension : string.Empty;
                                break;
                            case PropertyType.CriticalContext:
                                firstString = firstItem.ProjectIssue != null ? firstItem.ProjectIssue.isPerfCriticalContext.ToString() : string.Empty;
                                secondString = secondItem.ProjectIssue != null ? secondItem.ProjectIssue.isPerfCriticalContext.ToString() : string.Empty;
                                break;
                            case PropertyType.Severity:
                                firstString = firstItem.ProjectIssue != null ? firstItem.ProjectIssue.severity.ToString() : string.Empty;
                                secondString = secondItem.ProjectIssue != null ? secondItem.ProjectIssue.severity.ToString() : string.Empty;
                                break;
                            default:
                                var propertyIndex = property.type - PropertyType.Custom;
                                if (property.format == PropertyFormat.Integer || property.format == PropertyFormat.Bytes)
                                {
                                    int first;
                                    int second;
                                    if (firstItem.ProjectIssue == null || !int.TryParse(firstItem.ProjectIssue.GetCustomProperty(propertyIndex), out first))
                                        first = -999999;
                                    if (secondItem.ProjectIssue == null || !int.TryParse(secondItem.ProjectIssue.GetCustomProperty(propertyIndex), out second))
                                        second = -999999;
                                    return first - second;
                                }

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
