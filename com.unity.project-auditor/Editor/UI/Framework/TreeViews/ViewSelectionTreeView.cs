using System;
using System.Collections.Generic;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using UnityEditor;

namespace Unity.ProjectAuditor.Editor.UI.Framework
{
    class ViewSelectionTreeView : TreeView
    {
        public Action<Tab> OnSelectedNonAnalyzedTab;

        readonly Tab[] m_Tabs;
        readonly ViewManager m_ViewManager;
        readonly ProjectReport m_ProjectReport;

        Dictionary<int, IssueCategory> m_ItemIdToCategory = new Dictionary<int, IssueCategory>();
        Dictionary<IssueCategory, TreeViewItem> m_CategoryToItem = new Dictionary<IssueCategory, TreeViewItem>();
        Dictionary<int, Tab> m_ItemIdToTab = new Dictionary<int, Tab>();

        const float k_NonAnalyzedIconWidth = 16f;

        int m_CurrentlySelectedItemID;
        TreeViewItem m_RootItem;
        TreeViewItem m_FirstItem;
        GUIContent m_NonAnalyzedIcon;

        public ViewSelectionTreeView(TreeViewState treeViewState, Tab[] tabs,
                                     ViewManager viewManager,
                                     ProjectReport projectReport)
            : base(treeViewState)
        {
            m_Tabs = tabs;
            m_ViewManager = viewManager;
            m_ProjectReport = projectReport;

            m_NonAnalyzedIcon = Utility.GetIcon(Utility.IconType.AdditionalAnalysis, "Not Analyzed");

            Reload();
        }

        protected override bool CanMultiSelect(TreeViewItem item) => false;

        protected override TreeViewItem BuildRoot()
        {
            int id = 0;
            m_RootItem = new TreeViewItem { id = id++, depth = -1, displayName = "Root" };

            m_CategoryToItem.Clear();
            m_ItemIdToCategory.Clear();
            m_ItemIdToTab.Clear();

            foreach (var tab in m_Tabs)
            {
                if (tab.availableCategories == null || tab.availableCategories.Length == 0)
                    continue;

                var tabItem = new TreeViewItem { id = id++, displayName = tab.name };

                if (!m_RootItem.hasChildren)
                    m_FirstItem = tabItem;

                m_RootItem.AddChild(tabItem);

                m_ItemIdToTab.Add(tabItem.id, tab);

                // Only add child items if there's more than one view. Otherwise this item represents the only view.
                if (tab.availableCategories.Length > 1)
                {
                    var anyChildrenAnalyzed = false;
                    foreach (var childCategory in tab.availableCategories)
                    {
                        if (m_ProjectReport.HasCategory(childCategory))
                        {
                            anyChildrenAnalyzed = true;
                            break;
                        }
                    }

                    if (!anyChildrenAnalyzed)
                    {
                        // Increase the id anyway, so item ids stay same once we add more children later
                        id += tab.availableCategories.Length;
                        continue;
                    }

                    foreach (var cat in tab.availableCategories)
                    {
                        var view = m_ViewManager.GetView(cat);
                        if (view != null)
                        {
                            var categoryItem = new TreeViewItem { id = id++, displayName = view.Desc.DisplayName };
                            tabItem.AddChild(categoryItem);

                            m_ItemIdToCategory.Add(categoryItem.id, cat);
                            m_CategoryToItem.Add(cat, categoryItem);
                            m_ItemIdToTab.Add(categoryItem.id, tab);
                        }
                    }
                }
                else
                {
                    m_ItemIdToCategory.Add(tabItem.id, tab.availableCategories[0]);
                    m_CategoryToItem.Add(tab.availableCategories[0], tabItem);
                }
            }

            SetupDepthsFromParentsAndChildren(m_RootItem);

            return m_RootItem;
        }

        public override void OnGUI(Rect rect)
        {
            // Ensure we have an initial selection
            var selection = GetSelection();
            if (selection.Count == 0)
            {
                SelectItem(m_FirstItem);
            }

            if (Event.current.type == EventType.MouseDown || Event.current.type == EventType.KeyUp)
            {
                EditorApplication.delayCall += CheckNewSelection;
            }

            base.OnGUI(rect);
        }

        protected override void RowGUI(RowGUIArgs args)
        {
            base.RowGUI(args);

            Rect iconRect = new Rect(args.rowRect);
            iconRect.x = iconRect.xMax - k_NonAnalyzedIconWidth;
            iconRect.width = k_NonAnalyzedIconWidth;

            // Only show parent items as non-analyzed, not children (= views / categories)
            if (args.item.parent == m_RootItem && m_ProjectReport != null)
            {
                if (NeedsAnalysis(args.item))
                {
                    GUI.Label(iconRect, m_NonAnalyzedIcon, SharedStyles.LabelWithDynamicSize);
                }
            }
        }

        private bool NeedsAnalysis(TreeViewItem item)
        {
            if (m_ItemIdToTab.TryGetValue(item.id, out Tab foundTab))
            {
                foreach (var category in foundTab.availableCategories)
                {
                    if (!m_ProjectReport.HasCategory(category))
                    {
                        return true;
                    }
                }

                return false;
            }

            return true;
        }

        private void CheckNewSelection()
        {
            var selection = GetSelection();

            // We only use one selection at a time, so we chose the last one (even if user managed to do multiple selections)
            if (selection.Count > 0 && selection[0] != m_CurrentlySelectedItemID)
            {
                var item = FindItem(selection[0], rootItem);
                m_CurrentlySelectedItemID = selection[0];

                OnNewSelection(item, true, true);
            }
        }

        private void OnNewSelection(TreeViewItem item, bool changeView, bool expandItem)
        {
            if (expandItem)
            {
                var parent = item.parent;
                if (parent != null && parent != rootItem)
                    SetExpanded(item.parent.id, true);

                if (item.hasChildren && !IsExpanded(item.id))
                    SetExpanded(item.id, true);
            }

            if (changeView)
            {
                bool needsAnalysis = false;

                // Leaf items select a view using their category
                if (m_ItemIdToCategory.TryGetValue(item.id, out IssueCategory foundCategory))
                {
                    if (m_ProjectReport.HasCategory(foundCategory))
                        m_ViewManager.ChangeView(foundCategory);
                    else
                        needsAnalysis = true;
                }
                // Parent items with children select a view using their first child's category
                else if (item.hasChildren)
                {
                    if (m_ItemIdToCategory.TryGetValue(item.children[0].id, out IssueCategory foundChildCategory))
                    {
                        m_ViewManager.ChangeView(foundChildCategory);
                    }
                }
                else
                {
                    needsAnalysis = true;
                }

                if (needsAnalysis && m_ItemIdToTab.TryGetValue(item.id, out var foundTab))
                    OnSelectedNonAnalyzedTab(foundTab);
            }
        }

        private void SelectItem(TreeViewItem item, bool changeView = false, bool expandItem = true)
        {
            m_CurrentlySelectedItemID = item.id;

            SetSelection(new List<int> { m_CurrentlySelectedItemID });

            OnNewSelection(item, changeView, expandItem);
        }

        public void SelectItemByCategory(IssueCategory cat)
        {
            if (m_CategoryToItem.TryGetValue(cat, out TreeViewItem item))
            {
                SelectItem(item);
            }
        }
    }
}
