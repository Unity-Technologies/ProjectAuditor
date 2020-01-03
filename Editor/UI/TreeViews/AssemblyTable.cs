using UnityEngine;
using System;
using System.Collections.Generic;
using UnityEditor.IMGUI.Controls;
using UnityEngine.Assertions;
using System.Linq;
using UnityEditor;

namespace Unity.ProjectAuditor.Editor
{
    class AssemblyTreeViewItem : TreeViewItem
    {
        public readonly AssemblyIdentifier assemblyIdentifier;

        public AssemblyTreeViewItem(int id, int depth, string displayName, AssemblyIdentifier assemblyIdentifier) : base(id, depth, displayName)
        {
            this.assemblyIdentifier = assemblyIdentifier;
        }
    }

    class AssemblyTable : TreeView
    {
        const float kRowHeights = 20f;
        readonly List<TreeViewItem> m_Rows = new List<TreeViewItem>(100);

        private string[] m_AssemblyNames;
        AssemblyIdentifier m_AllAssemblyIdentifier;
        TreeViewSelection m_AssemblySelection;

        private GUIStyle m_ActiveLineStyle;

        // All columns
        public enum MyColumns
        {
            AssemblyName,
            State,
            GroupName
        }

        // SteveM TODO - Sorting doesn't work in this window (or in the Thread Selection Window in Profile Analyzer that
        // this is based on). So maybe rip this all out?
        public enum SortOption
        {
            AssemblyName,
            GroupName
        }

        // SteveM TODO - Sorting doesn't work in this window (or in the Thread Selection Window in Profile Analyzer that
        // this is based on). So maybe rip this all out?
        // Sort options per column
        SortOption[] m_SortOptions =
        {
            SortOption.AssemblyName,
            SortOption.AssemblyName,
            SortOption.GroupName
        };

        public AssemblyTable(TreeViewState state, MultiColumnHeader multicolumnHeader, string[] assemblyNames, TreeViewSelection assemblySelection) : base(state, multicolumnHeader)
        {
            m_AllAssemblyIdentifier = new AssemblyIdentifier();
            m_AllAssemblyIdentifier.SetName("All");
            m_AllAssemblyIdentifier.SetAll();

            Assert.AreEqual(m_SortOptions.Length, Enum.GetValues(typeof(MyColumns)).Length, "Ensure number of sort options are in sync with number of MyColumns enum values");

            // Custom setup
            rowHeight = kRowHeights;
            showAlternatingRowBackgrounds = true;
            showBorder = true;
            customFoldoutYOffset = (kRowHeights - EditorGUIUtility.singleLineHeight) * 0.5f; // center foldout in the row since we also center content. See RowGUI
            // extraSpaceBeforeIconAndLabel = 0;
            multicolumnHeader.sortingChanged += OnSortingChanged;

            m_AssemblyNames = assemblyNames;
            m_AssemblySelection = new TreeViewSelection(assemblySelection);
            Reload();
        }

        public void ClearAssemblySelection()
        {
            m_AssemblySelection.selection.Clear();
            m_AssemblySelection.groups.Clear();
            Reload();
        }

        public TreeViewSelection GetAssemblySelection()
        {
            return m_AssemblySelection;
        }

        protected int GetChildCount(AssemblyIdentifier selectedAssemblyIdentifier, out int selected)
        {
            int count = 0;
            int selectedCount = 0;

            if (selectedAssemblyIdentifier.index == AssemblyIdentifier.kAll)
            {
                if (selectedAssemblyIdentifier.name == "All")
                {
                    for (int index = 0; index < m_AssemblyNames.Length; ++index)
                    {
                        var assemblyNameWithIndex = m_AssemblyNames[index];
                        var assemblyIdentifier = new AssemblyIdentifier(assemblyNameWithIndex);

                        if (assemblyIdentifier.index != AssemblyIdentifier.kAll)
                        {
                            count++;
                            if (m_AssemblySelection.selection.Contains(assemblyNameWithIndex))
                                selectedCount++;
                        }
                    }
                }
                else
                {
                    for (int index = 0; index < m_AssemblyNames.Length; ++index)
                    {
                        var assemblyNameWithIndex = m_AssemblyNames[index];
                        var assemblyIdentifier = new AssemblyIdentifier(assemblyNameWithIndex);

                        if (selectedAssemblyIdentifier.name == assemblyIdentifier.name &&
                            assemblyIdentifier.index != AssemblyIdentifier.kAll)
                        {
                            count++;
                            if (m_AssemblySelection.selection.Contains(assemblyNameWithIndex))
                                selectedCount++;
                        }
                    }
                }
            }

            selected = selectedCount;
            return count;
        }

        protected override TreeViewItem BuildRoot()
        {
            int idForHiddenRoot = -1;
            int depthForHiddenRoot = -1;
            TreeViewItem root = new TreeViewItem(idForHiddenRoot, depthForHiddenRoot, "root");

            int depth = 0;

            var top = new AssemblyTreeViewItem(-1, depth, m_AllAssemblyIdentifier.name, m_AllAssemblyIdentifier);
            root.AddChild(top);

            var expandList = new List<int>() {-1};
            string lastAssemblyName = "";
            TreeViewItem node = root;
            for (int index = 0; index < m_AssemblyNames.Length; ++index)
            {
                var assemblyNameWithIndex = m_AssemblyNames[index];
                if (assemblyNameWithIndex == m_AllAssemblyIdentifier.assemblyNameWithIndex)
                    continue;

                var assemblyIdentifier = new AssemblyIdentifier(assemblyNameWithIndex);
                var item = new AssemblyTreeViewItem(index, depth, m_AssemblyNames[index], assemblyIdentifier);

                if (assemblyIdentifier.name != lastAssemblyName)
                {
                    // New assemblies at root
                    node = top;
                    depth = 0;
                }

                node.AddChild(item);

                if (assemblyIdentifier.name != lastAssemblyName)
                {
                    // Extra instances hang of the parent
                    lastAssemblyName = assemblyIdentifier.name;
                    node = item;
                    depth = 1;
                }
            }
            
            SetExpanded(expandList);
            
            SetupDepthsFromParentsAndChildren(root);

            return root;
        }
        
        private void BuildRowRecursive(IList<TreeViewItem> rows, TreeViewItem item)
        {
            if (!IsExpanded(item.id))
                return;

            foreach (AssemblyTreeViewItem subNode in item.children)
            {
                rows.Add(subNode);

                if (subNode.children!=null)
                    BuildRowRecursive(rows, subNode);
            }
        }

        private void BuildAllRows(IList<TreeViewItem> rows, TreeViewItem rootItem)
        {
            rows.Clear();
            if (rootItem == null)
                return;

            if (rootItem.children == null)
                return;

            foreach (AssemblyTreeViewItem node in rootItem.children)
            {
                rows.Add(node);

                if (node.children != null)
                    BuildRowRecursive(rows, node);
            }
        }

        protected override IList<TreeViewItem> BuildRows(TreeViewItem root)
        {
            BuildAllRows(m_Rows, root);

            SortIfNeeded(m_Rows);

            return m_Rows;
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

            // Sort the roots of the existing tree items
            SortByMultipleColumns();
            
            BuildAllRows(rows, rootItem);

            Repaint();
        }

        string GetItemGroupName(AssemblyTreeViewItem item)
        {
            string[] tokens = item.assemblyIdentifier.name.Split('.');
            if (tokens.Length <= 1)
            {
                return "";
            }

            return tokens[0];
        }

        void SortByMultipleColumns()
        {
            int[] sortedColumns = multiColumnHeader.state.sortedColumns;

            if (sortedColumns.Length == 0)
            {
                return;
            }

            var myTypes = rootItem.children.Cast<AssemblyTreeViewItem>();
            var orderedQuery = InitialOrder(myTypes, sortedColumns);
            for (int i = 1; i < sortedColumns.Length; i++)
            {
                SortOption sortOption = m_SortOptions[sortedColumns[i]];
                bool ascending = multiColumnHeader.IsSortedAscending(sortedColumns[i]);

                switch (sortOption)
                {
                    case SortOption.GroupName:
                        orderedQuery = orderedQuery.ThenBy(l => GetItemGroupName(l), ascending);
                        break;
                    case SortOption.AssemblyName:
                        orderedQuery = orderedQuery.ThenBy(l => l.displayName, ascending);
                        break;
                }
            }

            rootItem.children = orderedQuery.Cast<TreeViewItem>().ToList();
        }

        IOrderedEnumerable<AssemblyTreeViewItem> InitialOrder(IEnumerable<AssemblyTreeViewItem> myTypes, int[] history)
        {
            SortOption sortOption = m_SortOptions[history[0]];
            bool ascending = multiColumnHeader.IsSortedAscending(history[0]);
            switch (sortOption)
            {
                case SortOption.GroupName:
                    return myTypes.Order(l => GetItemGroupName(l), ascending);
                case SortOption.AssemblyName:
                    return myTypes.Order(l => l.displayName, ascending);
                default:
                    Assert.IsTrue(false, "Unhandled enum");
                    break;
            }

            // default
            return myTypes.Order(l => l.displayName, ascending);
        }

        protected override void RowGUI(RowGUIArgs args)
        {
            AssemblyTreeViewItem item = (AssemblyTreeViewItem)args.item;

            for (int i = 0; i < args.GetNumVisibleColumns(); ++i)
            {
                CellGUI(args.GetCellRect(i), item, (MyColumns)args.GetColumn(i), ref args);
            }
        }

        bool AssemblySelected(AssemblyIdentifier selectedAssemblyIdentifier)
        {
            if (m_AssemblySelection.selection != null &&
                m_AssemblySelection.selection.Count > 0 &&
                m_AssemblySelection.selection.Contains(selectedAssemblyIdentifier.assemblyNameWithIndex))
                return true;

            // If querying the 'All' filter then check if all selected
            if (selectedAssemblyIdentifier.assemblyNameWithIndex == m_AllAssemblyIdentifier.assemblyNameWithIndex)
            {
                // Check all assemblys without All in the name are selected
                foreach (var assemblyNameWithIndex in m_AssemblyNames)
                {
                    var assemblyIdentifier = new AssemblyIdentifier(assemblyNameWithIndex);
                    if (assemblyIdentifier.index == AssemblyIdentifier.kAll/* || assemblyIdentifier.index == AssemblyIdentifier.kSingle*/)
                        continue;

                    if (!m_AssemblySelection.selection.Contains(assemblyNameWithIndex))
                        return false;
                }

                return true;
            }

            // Need to check 'all' and assembly group all.
            if (selectedAssemblyIdentifier.index == AssemblyIdentifier.kAll)
            {
                // Count all assemblys that match this assembly group
                int count = 0;
                foreach (var assemblyNameWithIndex in m_AssemblyNames)
                {
                    var assemblyIdentifier = new AssemblyIdentifier(assemblyNameWithIndex);
                    if (assemblyIdentifier.index == AssemblyIdentifier.kAll || assemblyIdentifier.index == AssemblyIdentifier.kSingle)
                        continue;
                    
                    if (selectedAssemblyIdentifier.name != assemblyIdentifier.name)
                        continue;

                    count++;
                }

                // Count all the assemblys we have selected that match this assembly group
                int selectedCount = 0;
                foreach (var assemblyNameWithIndex in m_AssemblySelection.selection)
                {
                    var assemblyIdentifier = new AssemblyIdentifier(assemblyNameWithIndex);
                    if (selectedAssemblyIdentifier.name != assemblyIdentifier.name)
                        continue;
                    if (assemblyIdentifier.index > count)
                        continue;

                    selectedCount++;
                }

                if (count == selectedCount)
                    return true;
            }

            return false;
        }


        void CellGUI(Rect cellRect, AssemblyTreeViewItem item, MyColumns column, ref RowGUIArgs args)
        {
            // Center cell rect vertically (makes it easier to place controls, icons etc in the cells)
            CenterRectUsingSingleLineHeight(ref cellRect);

            switch (column)
            {
                case MyColumns.AssemblyName:
                    {
                        args.rowRect = cellRect;
                        // base.RowGUI(args);    // Required to show tree indenting

                        // Draw manually to keep indenting by add a tooltip
                        Rect rect = cellRect;
                        if (Event.current.rawType == EventType.Repaint)
                        {
                            int selectedChildren;
                            int childCount = GetChildCount(item.assemblyIdentifier, out selectedChildren);

                            string text;
                            string tooltip;
                            string fullAssemblyName = item.assemblyIdentifier.name;
                            string groupName = GetItemGroupName(item);

                            if (childCount <= 1)
                            {
                                text = item.displayName;
                                tooltip = (groupName == "") ? text : string.Format("{0}\n{1}", text, groupName);
                            }
                            else if (selectedChildren != childCount)
                            {
                                text = string.Format("{0} ({1} of {2})", fullAssemblyName, selectedChildren, childCount);
                                tooltip = (groupName == "") ? text : string.Format("{0}\n{1}", text, groupName);
                            }
                            else
                            {
                                text = string.Format("{0} (All)", fullAssemblyName);
                                tooltip = (groupName == "") ? text : string.Format("{0}\n{1}", text, groupName);
                            }
                            var content = new GUIContent(text, tooltip);

                            if (m_ActiveLineStyle == null)
                            {
                                m_ActiveLineStyle = new GUIStyle(DefaultStyles.label);
                                m_ActiveLineStyle.normal.textColor = DefaultStyles.boldLabel.onActive.textColor;
                            }
                       
                            // The rect is assumed indented and sized after the content when pinging
                            float indent = GetContentIndent(item) + extraSpaceBeforeIconAndLabel;
                            rect.xMin += indent;

                            int iconRectWidth = 16;
                            int kSpaceBetweenIconAndText = 2;

                            // Draw icon
                            Rect iconRect = rect;
                            iconRect.width = iconRectWidth;
                            // iconRect.x += 7f;

                            Texture icon = args.item.icon;
                            if (icon != null)
                                GUI.DrawTexture(iconRect, icon, ScaleMode.ScaleToFit);

                            rect.xMin += icon == null ? 0 : iconRectWidth + kSpaceBetweenIconAndText;

                            //bool mouseOver = rect.Contains(Event.current.mousePosition);
                            //DefaultStyles.label.Draw(rect, content, mouseOver, false, args.selected, args.focused);

                            // Must use this call to draw tooltip
                            EditorGUI.LabelField(rect, content, args.selected ? m_ActiveLineStyle : DefaultStyles.label);
                        }
                    }
                    break;
                case MyColumns.GroupName:
                    {
                        string groupName = GetItemGroupName(item);
                        var content = new GUIContent(groupName, groupName);
                        EditorGUI.LabelField(cellRect, content);
                    }
                    break;
                case MyColumns.State:
                    bool oldState = AssemblySelected(item.assemblyIdentifier);
                    bool newState = EditorGUI.Toggle(cellRect, oldState);
                    if (newState != oldState)
                    {
                        if (item.assemblyIdentifier.assemblyNameWithIndex == m_AllAssemblyIdentifier.assemblyNameWithIndex)
                        {
                            // Record active groups
                            m_AssemblySelection.groups.Clear();
                            if (newState)
                            {
                                if (!m_AssemblySelection.groups.Contains(item.assemblyIdentifier.assemblyNameWithIndex))
                                    m_AssemblySelection.groups.Add(item.assemblyIdentifier.assemblyNameWithIndex);
                            }

                            // Update selection
                            m_AssemblySelection.selection.Clear();
                            if (newState)
                            {
                                foreach (string assemblyNameWithIndex in m_AssemblyNames)
                                {
                                    if (assemblyNameWithIndex != m_AllAssemblyIdentifier.assemblyNameWithIndex)
                                    {
                                        var assemblyIdentifier = new AssemblyIdentifier(assemblyNameWithIndex);
                                        if (assemblyIdentifier.index != AssemblyIdentifier.kAll)
                                        {
                                            m_AssemblySelection.selection.Add(assemblyNameWithIndex);
                                        }
                                    }
                                }
                            }
                        }
                        else if (item.assemblyIdentifier.index == AssemblyIdentifier.kAll)
                        {
                            // Record active groups
                            if (newState)
                            {
                                if (!m_AssemblySelection.groups.Contains(item.assemblyIdentifier.assemblyNameWithIndex))
                                    m_AssemblySelection.groups.Add(item.assemblyIdentifier.assemblyNameWithIndex);
                            }
                            else
                            {
                                m_AssemblySelection.groups.Remove(item.assemblyIdentifier.assemblyNameWithIndex);
                                // When turning off a sub group, turn of the 'all' group too
                                m_AssemblySelection.groups.Remove(m_AllAssemblyIdentifier.assemblyNameWithIndex);
                            }

                            // Update selection
                            if (newState)
                            {
                                foreach (string assemblyNameWithIndex in m_AssemblyNames)
                                {
                                    var assemblyIdentifier = new AssemblyIdentifier(assemblyNameWithIndex);
                                    if (assemblyIdentifier.name == item.assemblyIdentifier.name &&
                                        assemblyIdentifier.index != AssemblyIdentifier.kAll)
                                    {
                                        if (!m_AssemblySelection.selection.Contains(assemblyNameWithIndex))
                                            m_AssemblySelection.selection.Add(assemblyNameWithIndex);
                                    }
                                }
                            }
                            else
                            {
                                var removeSelection = new List<string>();
                                foreach (string assemblyNameWithIndex in m_AssemblySelection.selection)
                                {
                                    var assemblyIdentifier = new AssemblyIdentifier(assemblyNameWithIndex);
                                    if (assemblyIdentifier.name == item.assemblyIdentifier.name &&
                                        assemblyIdentifier.index != AssemblyIdentifier.kAll)
                                    {
                                        removeSelection.Add(assemblyNameWithIndex);
                                    }
                                }
                                foreach (string assemblyNameWithIndex in removeSelection)
                                {
                                    m_AssemblySelection.selection.Remove(assemblyNameWithIndex);
                                }
                            }
                        }
                        else
                        {
                            if (newState)
                            {
                                m_AssemblySelection.selection.Add(item.assemblyIdentifier.assemblyNameWithIndex);
                            }
                            else
                            {
                                m_AssemblySelection.selection.Remove(item.assemblyIdentifier.assemblyNameWithIndex);

                                // Turn off any group its in too
                                var groupIdentifier = new AssemblyIdentifier(item.assemblyIdentifier);
                                groupIdentifier.SetAll();
                                m_AssemblySelection.groups.Remove(groupIdentifier.assemblyNameWithIndex);

                                // Turn of the 'all' group too
                                m_AssemblySelection.groups.Remove(m_AllAssemblyIdentifier.assemblyNameWithIndex);
                            }
                        }
                    }
                    break;
            }
        }


        // Misc
        //--------

        protected override bool CanMultiSelect(TreeViewItem item)
        {
            return false;
        }

        struct HeaderData
        {
            public GUIContent content;
            public float width;
            public float minWidth;
            public bool autoResize;
            public bool allowToggleVisibility;

            public HeaderData(string name, string tooltip = "", float _width = 50, float _minWidth = 30, bool _autoResize = true, bool _allowToggleVisibility = true)
            {
                content = new GUIContent(name, tooltip);
                width = _width;
                minWidth = _minWidth;
                autoResize = _autoResize;
                allowToggleVisibility = _allowToggleVisibility;
            }
        }

        public static MultiColumnHeaderState CreateDefaultMultiColumnHeaderState(float treeViewWidth)
        {
            var columnList = new List<MultiColumnHeaderState.Column>();
            HeaderData[] headerData = new HeaderData[]
            {
                new HeaderData("Assembly", "Assembly Name", 350, 100, true, false),
                new HeaderData("Show", "Check to show this assembly in the analysis views", 40, 100, false, false),
                new HeaderData("Group", "Assembly Group", 100, 100, true, false),

            };
            foreach (var header in headerData)
            {
                columnList.Add(new MultiColumnHeaderState.Column
                {
                    headerContent = header.content,
                    headerTextAlignment = TextAlignment.Left,
                    sortedAscending = true,
                    sortingArrowAlignment = TextAlignment.Left,
                    width = header.width,
                    minWidth = header.minWidth,
                    autoResize = header.autoResize,
                    allowToggleVisibility = header.allowToggleVisibility
                });
            };
            var columns = columnList.ToArray();

            Assert.AreEqual(columns.Length, Enum.GetValues(typeof(MyColumns)).Length, "Number of columns should match number of enum values: You probably forgot to update one of them.");

            var state = new MultiColumnHeaderState(columns);
            state.visibleColumns = new int[] {
                        (int)MyColumns.AssemblyName,
                        (int)MyColumns.State,
                        //(int)MyColumns.GroupName
                    };
            return state;
        }

//        protected override void SelectionChanged(IList<int> selectedIds)
//        {
//            base.SelectionChanged(selectedIds);
//
//            if (selectedIds.Count > 0)
//            {
//            }
//        }
    }

    
    // SteveM TODO - Can ditch this if we ditch sorting.
    static class MyExtensionMethods
    {
        public static IOrderedEnumerable<T> Order<T, TKey>(this IEnumerable<T> source, Func<T, TKey> selector, bool ascending)
        {
            if (ascending)
            {
                return source.OrderBy(selector);
            }
            else
            {
                return source.OrderByDescending(selector);
            }
        }

        public static IOrderedEnumerable<T> ThenBy<T, TKey>(this IOrderedEnumerable<T> source, Func<T, TKey> selector, bool ascending)
        {
            if (ascending)
            {
                return source.ThenBy(selector);
            }
            else
            {
                return source.ThenByDescending(selector);
            }
        }
    }
}
