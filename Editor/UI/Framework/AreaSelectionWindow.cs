using UnityEditor.IMGUI.Controls;

namespace Unity.ProjectAuditor.Editor.UI.Framework
{
    class AreaSelectionWindow : SelectionWindow
    {
        protected override void CreateTable(TreeViewSelection selection, string[] names)
        {
            if (m_TreeViewState == null)
                m_TreeViewState = new TreeViewState();

            MultiSelectionTable.HeaderData[] headerData =
            {
                new MultiSelectionTable.HeaderData("Areas", "Area Names", 350, 100, true, false),
                new MultiSelectionTable.HeaderData("Show",
                    "Check to show issues affecting this area in the analysis views", 40, 100, false, false),
                new MultiSelectionTable.HeaderData("Group", "Group", 100, 100, true, false)
            };
            m_MultiColumnHeaderState = MultiSelectionTable.CreateDefaultMultiColumnHeaderState(headerData);

            var multiColumnHeader = new MultiColumnHeader(m_MultiColumnHeaderState);
            multiColumnHeader.SetSorting((int)MultiSelectionTable.Column.ItemName, true);
            multiColumnHeader.ResizeToFit();
            m_SelectionTable = new MultiSelectionTable(m_TreeViewState, multiColumnHeader, names, selection);
        }
    }
}
