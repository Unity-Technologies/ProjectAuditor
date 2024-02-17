using System.Collections.Generic;
using System.Linq;
using Unity.ProjectAuditor.Editor.Core;
using Unity.ProjectAuditor.Editor.Modules;
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

        readonly SeverityRules m_Rules;
        readonly ViewDescriptor m_Desc;
        readonly AnalysisView m_View;
        readonly IssueLayout m_Layout;
        readonly List<TreeViewItem> m_Rows = new List<TreeViewItem>(100);

        List<IssueTableItem> m_TreeViewItemGroups = new List<IssueTableItem>();
        Dictionary<int, IssueTableItem> m_TreeViewItemIssues;
        List<IssueTableItem> m_SelectedIssues = new List<IssueTableItem>();
        bool m_SelectionChanged = true;
        int m_NextId;
        int m_NumMatchingIssues;
        bool m_FlatView;
        bool m_ShowIgnoredIssues;
        int m_GroupPropertyIndex;

        public bool flatView
        {
            get => m_FlatView;
            set => m_FlatView = value;
        }

        public bool showIgnoredIssues
        {
            get => m_ShowIgnoredIssues;
            set => m_ShowIgnoredIssues = value;
        }

        public int groupPropertyIndex
        {
            get => m_GroupPropertyIndex;
            set
            {
                if (value >= m_Layout.Properties.Length)
                    return;
                if (value >= 0)
                    m_GroupPropertyIndex = value;
            }
        }

        public IssueTable(TreeViewState state, MultiColumnHeader multicolumnHeader,
                          ViewDescriptor desc, IssueLayout layout, SeverityRules rules,
                          AnalysisView view) : base(state,
                                                    multicolumnHeader)
        {
            m_Rules = rules;
            m_View = view;
            m_Desc = desc;
            m_Layout = layout;
            m_FlatView = true; // by default, don't use groups

            var propertyIndex = m_Layout.DefaultGroupPropertyIndex;
            if (propertyIndex != -1)
            {
                m_FlatView = false;
                m_GroupPropertyIndex = propertyIndex;
            }

            multicolumnHeader.sortingChanged += OnSortingChanged;
            showAlternatingRowBackgrounds = true;

            Clear();
        }

        public void AddIssues(IReadOnlyCollection<ReportItem> issues)
        {
            // update groups
            var groupNames = issues.Select(i => i.GetPropertyGroup(m_Layout.Properties[m_GroupPropertyIndex])).Distinct().ToArray();
            foreach (var name in groupNames)
            {
                // if necessary, create a group
                if (!m_TreeViewItemGroups.Exists(g => g.GroupName.Equals(name)))
                    m_TreeViewItemGroups.Add((new IssueTableItem(m_NextId++, 0, name)));
            }

            var items = new Dictionary<int, IssueTableItem>(issues.Count);
            if (m_TreeViewItemIssues != null)
            {
                foreach (var issuesPair in m_TreeViewItemIssues)
                {
                    items.Add(issuesPair.Value.id, issuesPair.Value);
                }
            }

            foreach (var issue in issues)
            {
                var depth = 1;
                if (m_Layout.IsHierarchy)
                {
                    if (m_Desc.Category == IssueCategory.BuildStep)
                    {
                        depth = issue.GetCustomPropertyInt32(BuildReportStepProperty.Depth);
                    }
                    else
                        depth = 0;
                }

                var item = new IssueTableItem(m_NextId++, depth, issue.Description, issue, issue.GetPropertyGroup(m_Layout.Properties[m_GroupPropertyIndex]));
                items.Add(item.id, item);
            }

            m_TreeViewItemIssues = items;
        }

        public void Clear()
        {
            m_NextId = k_FirstId;
            m_TreeViewItemGroups.Clear();
            m_TreeViewItemIssues = new Dictionary<int, IssueTableItem>();
            ClearSelection();
        }

        protected override TreeViewItem BuildRoot()
        {
            var idForHiddenRoot = -1;
            var depthForHiddenRoot = -1;
            var root = new TreeViewItem(idForHiddenRoot, depthForHiddenRoot, "root");

            foreach (var item in m_TreeViewItemGroups)
            {
                root.AddChild(item);
            }

            return root;
        }

        protected override IList<TreeViewItem> BuildRows(TreeViewItem root)
        {
            m_Rows.Clear();

            // find all issues matching the filters and make an array out of them
            Profiler.BeginSample("IssueTable.Match");
            var filteredItems = m_TreeViewItemIssues.Where(item => m_View.Match(item.Value.ReportItem)).ToArray();
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
                var groupedItemQuery = filteredItems.GroupBy(i => i.Value.ReportItem.GetPropertyGroup(m_Layout.Properties[m_GroupPropertyIndex]));
                foreach (var groupedItems in groupedItemQuery)
                {
                    var groupName = groupedItems.Key;
                    var group = m_TreeViewItemGroups.Find(g => g.GroupName.Equals(groupName));
                    m_Rows.Add(group);

                    var groupIsExpanded = state.expandedIDs.Contains(group.id);
                    var children = filteredItems.Where(item => item.Value.GroupName.Equals(groupName));

                    group.NumVisibleChildren = children.Count();
                    group.DisplayName = groupName;

                    foreach (var child in children)
                    {
                        if (groupIsExpanded)
                            m_Rows.Add(child.Value);
                        group.AddChild(child.Value);
                    }
                }
            }
            else
            {
                foreach (var item in filteredItems)
                {
                    var group = m_TreeViewItemGroups.Find(g => g.GroupName.Equals(item.Value.GroupName));
                    group.AddChild(item.Value);

                    m_Rows.Add(item.Value);
                }
            }
            SortIfNeeded(m_Rows);

            Profiler.EndSample();

            return m_Rows;
        }

        protected override IList<int> GetAncestors(int id)
        {
            if (m_TreeViewItemIssues == null || m_TreeViewItemIssues.Count == 0)
                return new List<int>();
            return base.GetAncestors(id);
        }

        protected override IList<int> GetDescendantsThatHaveChildren(int id)
        {
            if (m_TreeViewItemIssues == null || m_TreeViewItemIssues.Count == 0)
                return new List<int>();
            return base.GetDescendantsThatHaveChildren(id);
        }

        public void SetFontSize(int fontSize)
        {
            rowHeight = k_DefaultRowHeight * fontSize / ViewStates.DefaultMinFontSize;
        }

        protected override void RowGUI(RowGUIArgs args)
        {
            for (var i = 0; i < args.GetNumVisibleColumns(); ++i)
                CellGUI(args.GetCellRect(i), args.item, args.GetColumn(i), ref args);
        }

        public string GetCustomGroupPropertyCellString(IssueTableItem item, PropertyDefinition property)
        {
            string label = null;
            var customPropertyIndex = PropertyTypeUtil.ToCustomIndex(property.Type);
            if (property.Format == PropertyFormat.Bytes || property.Format == PropertyFormat.Time || property.Format == PropertyFormat.Percentage)
            {
                if (property.Format == PropertyFormat.Bytes)
                {
                    ulong sum = 0;
                    foreach (var childItem in item.children)
                    {
                        var issueTableItem = childItem as IssueTableItem;
                        var value = issueTableItem.ReportItem.GetCustomPropertyUInt64(customPropertyIndex);
                        sum += value;
                    }

                    label = Formatting.FormatSize(sum);
                }
                else
                {
                    float sum = 0;
                    foreach (var childItem in item.children)
                    {
                        var issueTableItem = childItem as IssueTableItem;
                        var value = issueTableItem.ReportItem.GetCustomPropertyFloat(customPropertyIndex);
                        sum += value;
                    }
                    label = property.Format == PropertyFormat.Time ? Formatting.FormatTime(sum) : Formatting.FormatPercentage(sum, 1);
                }
            }

            return label;
        }

        void CellGUI(Rect cellRect, TreeViewItem treeViewItem, int columnIndex, ref RowGUIArgs args)
        {
            var property = m_Layout.Properties[columnIndex];
            if (property.IsHidden)
                return;

            var propertyType = property.Type;
            var labelStyle = SharedStyles.LabelWithDynamicSize;
            var item = treeViewItem as IssueTableItem;

            if (item == null)
            {
                if (propertyType == PropertyType.Description)
                    EditorGUI.LabelField(cellRect, new GUIContent(treeViewItem.displayName, treeViewItem.displayName), labelStyle);
                return;
            }

            var contentIndent = GetContentIndent(treeViewItem);
            // indent first column, if necessary
            if (columnIndex == 0 && !hasSearch && !m_FlatView)
            {
                var indent = contentIndent + extraSpaceBeforeIconAndLabel;
                cellRect.xMin += indent;
                CenterRectUsingSingleLineHeight(ref cellRect);
            }
            else if (m_Layout.IsHierarchy && property.Type == PropertyType.Description)
            {
                var indent = contentIndent;
                cellRect.xMin += indent;
                CenterRectUsingSingleLineHeight(ref cellRect);
            }

            if (item.IsGroup())
            {
                if (columnIndex == 0)
                {
                    // use all available space to display description
                    cellRect.xMax = args.rowRect.xMax;

                    var guiContent = new GUIContent(item.GetDisplayName());
                    EditorGUI.LabelField(cellRect, guiContent, labelStyle);

                    EditorGUI.LabelField(new Rect(cellRect)
                    {
                        x = labelStyle.CalcSize(guiContent).x + contentIndent
                    }, $"({item.NumVisibleChildren} Items)", SharedStyles.LabelDarkWithDynamicSize);
                }
                else if (PropertyTypeUtil.IsCustom(property.Type))
                {
                    string label = GetCustomGroupPropertyCellString(item, property);

                    if (!string.IsNullOrEmpty(label))
                    {
                        EditorGUI.LabelField(cellRect, label, labelStyle);
                    }
                }
            }
            else
            {
                Rule rule = null;
                var issue = item.ReportItem;
                if (issue.WasFixed)
                    GUI.enabled = false;
                else if (issue.Id.IsValid())
                {
                    var id = issue.Id;
                    rule = m_Rules.GetRule(id, issue.GetContext());
                    if (rule == null)
                        rule = m_Rules.GetRule(id); // try to find non-specific rule
                    if (rule != null && rule.Severity == Severity.None)
                        GUI.enabled = false;
                }

                switch (propertyType)
                {
                    case PropertyType.LogLevel:
                    {
                        if (issue.Severity != Severity.Hidden)
                        {
                            var icon = Utility.GetLogLevelIcon(issue.LogLevel);
                            if (icon != null)
                            {
                                EditorGUI.LabelField(cellRect, icon, labelStyle);
                            }
                        }
                    }
                    break;

                    case PropertyType.Severity:
                    {
                        EditorGUI.LabelField(cellRect, Utility.GetSeverityIconWithText(issue.Severity), labelStyle);
                    }
                    break;

                    case PropertyType.Areas:
                        var areaNames = issue.Id.GetDescriptor().GetAreasSummary();
                        EditorGUI.LabelField(cellRect, new GUIContent(areaNames, Tooltip.Area), labelStyle);
                        break;
                    case PropertyType.Description:
                        GUIContent guiContent = null;
                        if (issue.Location != null && m_Desc.DescriptionWithIcon)
                        {
                            guiContent =
                                Utility.GetTextContentWithAssetIcon(item.GetDisplayName(), issue.Location.Path);
                        }
                        else
                        {
                            guiContent = new GUIContent(item.GetDisplayName(), item.GetDisplayName());
                        }
                        EditorGUI.LabelField(cellRect, guiContent, labelStyle);
                        break;

                    case PropertyType.Filename:
                        // display fullpath as tooltip
                        EditorGUI.LabelField(cellRect, new GUIContent(issue.GetProperty(PropertyType.Filename), issue.GetProperty(PropertyType.Path)), labelStyle);
                        break;

                    default:
                        if (PropertyTypeUtil.IsCustom(propertyType))
                        {
                            var customPropertyIndex = PropertyTypeUtil.ToCustomIndex(propertyType);

                            switch (property.Format)
                            {
                                case PropertyFormat.Bool:
                                    var boolAsString = issue.GetCustomProperty(customPropertyIndex);
                                    var boolValue = false;
                                    if (!bool.TryParse(boolAsString, out boolValue))
                                        EditorGUI.LabelField(cellRect, Utility.GetIcon(Utility.IconType.Info, boolAsString), labelStyle);
                                    else if (boolValue)
                                        EditorGUI.LabelField(cellRect, Utility.GetIcon(Utility.IconType.WhiteCheckMark), labelStyle);
                                    break;
                                case PropertyFormat.Bytes:
                                    EditorGUI.LabelField(cellRect, Formatting.FormatSize(issue.GetCustomPropertyUInt64(customPropertyIndex)), labelStyle);
                                    break;
                                case PropertyFormat.Time:
                                    EditorGUI.LabelField(cellRect, Formatting.FormatTime(issue.GetCustomPropertyFloat(customPropertyIndex)), labelStyle);
                                    break;
                                case PropertyFormat.ULong:
                                    var ulongAsString = issue.GetCustomProperty(customPropertyIndex);
                                    var ulongValue = (ulong)0;
                                    if (!ulong.TryParse(ulongAsString, out ulongValue))
                                        EditorGUI.LabelField(cellRect, Utility.GetIcon(Utility.IconType.Info, ulongAsString), labelStyle);
                                    else
                                        EditorGUI.LabelField(cellRect, new GUIContent(ulongAsString, ulongAsString), labelStyle);
                                    break;
                                case PropertyFormat.Integer:
                                    var intAsString = issue.GetCustomProperty(customPropertyIndex);
                                    var intValue = 0;
                                    if (!int.TryParse(intAsString, out intValue))
                                        EditorGUI.LabelField(cellRect, Utility.GetIcon(Utility.IconType.Info, intAsString), labelStyle);
                                    else
                                        EditorGUI.LabelField(cellRect, new GUIContent(intAsString, intAsString), labelStyle);
                                    break;
                                case PropertyFormat.Percentage:
                                    EditorGUI.LabelField(cellRect, Formatting.FormatPercentage(issue.GetCustomPropertyFloat(customPropertyIndex), 1), labelStyle);
                                    break;
                                default:
                                    var value = issue.GetCustomProperty(customPropertyIndex);
                                    EditorGUI.LabelField(cellRect, new GUIContent(value, value), labelStyle);
                                    break;
                            }
                        }
                        else
                        {
                            EditorGUI.LabelField(cellRect, new GUIContent(issue.GetProperty(propertyType)), labelStyle);
                        }

                        break;
                }
                if (issue.WasFixed)
                    GUI.enabled = true;
                else if (rule != null && rule.Severity == Severity.None)
                    GUI.enabled = true;
            }

            ShowContextMenu(cellRect, item, propertyType);
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
            if (m_Desc.OnOpenIssue == null)
                return;

            var rows = FindRows(new[] {id});
            var item = rows.FirstOrDefault();

            if (item == null)
                return;

            var tableItem = item as IssueTableItem;

            if (tableItem == null)
                return;

            var issue = tableItem.ReportItem;
            if (issue != null && issue.Location != null && issue.Location.IsValid)
            {
                m_Desc.OnOpenIssue(issue.Location);
            }
        }

        protected override void SearchChanged(string newSearch)
        {
            // auto-expand groups containing selected items
            foreach (var id in state.selectedIDs)
            {
                var item = m_TreeViewItemIssues.FirstOrDefault(issue => issue.Value.id == id && issue.Value.parent != null);
                if (item.Value != null && !state.expandedIDs.Contains(item.Value.parent.id))
                {
                    state.expandedIDs.Add(item.Value.parent.id);
                }
            }
        }

        public int GetNumMatchingIssues()
        {
            return m_NumMatchingIssues;
        }

        public List<IssueTableItem> GetSelectedItems()
        {
            if (!m_SelectionChanged)
            {
                return m_SelectedIssues;
            }

            m_SelectionChanged = false;

            var ids = GetSelection();

            m_SelectedIssues.Clear();

            var count = ids.Count();
            if (count > 0)
            {
                for (int i = 0; i < count; ++i)
                {
                    // Skip group rows that are not in the dictionary
                    if (m_TreeViewItemIssues.TryGetValue(ids[i], out var item))
                        m_SelectedIssues.Add(item);
                }

                return m_SelectedIssues;
            }

            return m_SelectedIssues;
        }

        protected override void SelectionChanged(IList<int> selectedIds)
        {
            m_SelectionChanged = true;
        }

        void OnSortingChanged(MultiColumnHeader _multiColumnHeader)
        {
            if (m_Layout.IsHierarchy)
                return;

            SortIfNeeded(GetRows());
        }

        void ShowContextMenu(Rect cellRect, IssueTableItem item, PropertyType propertyType)
        {
            var current = Event.current;
            if (cellRect.Contains(current.mousePosition) && current.type == EventType.ContextClick)
            {
                var menu = new GenericMenu();

                menu.AddItem(Utility.ClearSelection, false, ClearSelection);

                if (item.ReportItem != null)
                {
                    if (m_Desc.OnOpenIssue != null && item.ReportItem.Location != null)
                    {
                        menu.AddItem(Utility.OpenIssue, false, () =>
                        {
                            m_Desc.OnOpenIssue(item.ReportItem.Location);
                        });
                    }
                    menu.AddItem(new GUIContent($"Filter by '{item.ReportItem.Description.Replace("/", "\u2215")}'") , false, () =>
                    {
                        m_View.SetSearch(item.ReportItem.Description);
                    });
                }

                if (m_Desc.OnOpenIssue != null && item.ReportItem != null && item.ReportItem.Location != null)
                {
                    menu.AddItem(Utility.OpenIssue, false, () =>
                    {
                        m_Desc.OnOpenIssue(item.ReportItem.Location);
                    });
                }

                var desc = item.ReportItem != null && item.ReportItem.Id.IsValid() ? item.ReportItem.Id.GetDescriptor() : null;
                if (m_Desc.OnOpenManual != null && desc != null && desc.Type.StartsWith("UnityEngine."))
                {
                    menu.AddItem(Utility.OpenScriptReference, false, () =>
                    {
                        m_Desc.OnOpenManual(desc);
                    });
                }

                if (m_Desc.OnContextMenu != null)
                {
                    menu.AddSeparator("");
                    m_Desc.OnContextMenu(menu, m_View.ViewManager, item.ReportItem);
                }

                menu.AddSeparator("");
                menu.AddItem(Utility.CopyToClipboard, false, () =>
                {
                    EditorInterop.CopyToClipboard(
                        item.IsGroup() ? item.GetDisplayName() : item.ReportItem.GetProperty(propertyType));
                });

                menu.ShowAsContext();

                current.Use();
            }
        }

        public void ClearSelection()
        {
            state.selectedIDs.Clear();

            m_SelectionChanged = true;
        }

        void SortIfNeeded(IList<TreeViewItem> rows)
        {
            if (rows == null || rows.Count <= 1)
                return;

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

                        if (a.m_Item.IsGroup() && b.m_Item.IsGroup())
                            rtn = order * CompareGroupItemTo(a.m_Item, b.m_Item, columnSortOrder[i]);
                        else
                            rtn = order * ProjectIssueExtensions.CompareTo(a.m_Item.ReportItem != null ? a.m_Item.ReportItem : null, b.m_Item.ReportItem != null ? b.m_Item.ReportItem : null, m_Layout.Properties[columnSortOrder[i]].Type);

                        if (rtn == 0)
                            continue;

                        return rtn;
                    }

                    return rtn;
                });

                foreach (var child in m_Children)
                    child.Sort(columnSortOrder, isColumnAscending);
            }

            int CompareGroupItemTo(IssueTableItem itemA, IssueTableItem itemB, int columnIndex)
            {
                if (columnIndex == 0)
                    return string.CompareOrdinal(itemA.GroupName, itemB.GroupName);

                var property = m_Layout.Properties[columnIndex];
                if (property.IsHidden)
                    return 0;

                var propertyType = property.Type;

                if (PropertyTypeUtil.IsCustom(property.Type))
                {
                    var customPropertyIndex = PropertyTypeUtil.ToCustomIndex(propertyType);
                    if (property.Format == PropertyFormat.Bytes)
                    {
                        var valueA = GetGroupColumnSumUlong(itemA, customPropertyIndex);
                        var valueB = GetGroupColumnSumUlong(itemB, customPropertyIndex);

                        return valueA > valueB ? 1 : (valueA < valueB ? -1 : 0);
                    }
                    if (property.Format == PropertyFormat.Time ||
                        property.Format == PropertyFormat.Percentage)
                    {
                        var valueA = GetGroupColumnSumFloat(itemA, customPropertyIndex);
                        var valueB = GetGroupColumnSumFloat(itemB, customPropertyIndex);

                        return valueA > valueB ? 1 : (valueA < valueB ? -1 : 0);
                    }

                    var stringA = GetGroupFirstChildCustomProperty(itemA, customPropertyIndex);
                    var stringB = GetGroupFirstChildCustomProperty(itemB, customPropertyIndex);
                    return string.CompareOrdinal(stringA, stringB);
                }
                else
                {
                    var stringA = GetGroupFirstChildProperty(itemA, property.Type);
                    var stringB = GetGroupFirstChildProperty(itemB, property.Type);
                    return string.CompareOrdinal(stringA, stringB);
                }
            }

            string GetGroupFirstChildCustomProperty(IssueTableItem item, int customPropertyIndex)
            {
                if (item.children.Count == 0)
                    return string.Empty;

                var issueTableItem = item.children[0] as IssueTableItem;
                return issueTableItem.ReportItem.GetCustomProperty(customPropertyIndex);
            }

            string GetGroupFirstChildProperty(IssueTableItem item, PropertyType propertyType)
            {
                if (item.children.Count == 0)
                    return string.Empty;

                var issueTableItem = item.children[0] as IssueTableItem;
                return issueTableItem.ReportItem.GetProperty(propertyType);
            }

            ulong GetGroupColumnSumUlong(IssueTableItem item, int customPropertyIndex)
            {
                ulong sum = 0;
                foreach (var childItem in item.children)
                {
                    var issueTableItem = childItem as IssueTableItem;
                    var value = issueTableItem.ReportItem.GetCustomPropertyUInt64(customPropertyIndex);
                    sum += value;
                }

                return sum;
            }

            float GetGroupColumnSumFloat(IssueTableItem item, int customPropertyIndex)
            {
                float sum = 0;
                foreach (var childItem in item.children)
                {
                    var issueTableItem = childItem as IssueTableItem;
                    var value = issueTableItem.ReportItem.GetCustomPropertyFloat(customPropertyIndex);
                    sum += value;
                }

                return sum;
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

        static class Tooltip
        {
            public static string Area = "Areas that this issue might have an impact on";
            public static string HotPath = "Potential hot-path";
        }
    }
}
