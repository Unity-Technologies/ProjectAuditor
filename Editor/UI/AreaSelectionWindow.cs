using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace Unity.ProjectAuditor.Editor
{
    internal class AreaSelectionWindow : EditorWindow
    {
        private MultiSelectionTable m_AreaTable;
        private MultiColumnHeaderState m_MultiColumnHeaderState;
        private ProjectAuditorWindow m_ProjectAuditorWindow;
        private TreeViewState m_TreeViewState;

        public static AreaSelectionWindow Open(float screenX, float screenY, ProjectAuditorWindow projectAuditorWindow,
            TreeViewSelection selection, string[] names)
        {
            var window = GetWindow<AreaSelectionWindow>("Areas");
            window.position = new Rect(screenX, screenY, 400, 500);
            window.SetData(projectAuditorWindow, selection, names);
            window.Show();

            return window;
        }

        public static void CloseAll()
        {
            var window = GetWindow<AreaSelectionWindow>("Areas");
            window.Close();
        }

        private void OnLostFocus()
        {
            Close();
        }

        private void OnDestroy()
        {
            m_ProjectAuditorWindow.SetAreaSelection(m_AreaTable.GetTreeViewSelection());
        }

        public static bool IsOpen()
        {
            var windows = Resources.FindObjectsOfTypeAll(typeof(AreaSelectionWindow));
            if (windows != null && windows.Length > 0)
                return true;

            return false;
        }

        private void SetData(ProjectAuditorWindow projectAuditorWindow, TreeViewSelection selection, string[] names)
        {
            m_ProjectAuditorWindow = projectAuditorWindow;
            CreateTable(projectAuditorWindow, selection, names);
        }

        private void CreateTable(ProjectAuditorWindow projectAuditorWindow, TreeViewSelection selection, string[] names)
        {
            if (m_TreeViewState == null)
                m_TreeViewState = new TreeViewState();

            MultiSelectionTable.HeaderData[] headerData =
            {
                new MultiSelectionTable.HeaderData("Area", "Area Name", 350, 100, true, false),
                new MultiSelectionTable.HeaderData("Show",
                    "Check to show issues affecting this area in the analysis views", 40, 100, false, false),
                new MultiSelectionTable.HeaderData("Group", "Group", 100, 100, true, false)
            };
            m_MultiColumnHeaderState = MultiSelectionTable.CreateDefaultMultiColumnHeaderState(headerData);

            var multiColumnHeader = new MultiColumnHeader(m_MultiColumnHeaderState);
            multiColumnHeader.SetSorting((int)MultiSelectionTable.MyColumns.ItemName, true);
            multiColumnHeader.ResizeToFit();
            m_AreaTable = new MultiSelectionTable(m_TreeViewState, multiColumnHeader, names, selection);
        }

        private void OnGUI()
        {
            EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true));
            var style = new GUIStyle(GUI.skin.label);
            style.alignment = TextAnchor.MiddleLeft;
            GUILayout.Label("Select Area : ", style);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Clear", GUILayout.Width(50))) m_AreaTable.ClearSelection();
            if (GUILayout.Button("Apply", GUILayout.Width(50)))
                m_ProjectAuditorWindow.SetAreaSelection(m_AreaTable.GetTreeViewSelection());
            EditorGUILayout.EndHorizontal();

            if (m_AreaTable != null)
            {
                var r = EditorGUILayout.GetControlRect(GUILayout.ExpandHeight(true));
                m_AreaTable.OnGUI(r);
            }

            EditorGUILayout.EndVertical();
        }
    }
}
