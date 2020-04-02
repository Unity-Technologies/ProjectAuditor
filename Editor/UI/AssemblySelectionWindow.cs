using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace Unity.ProjectAuditor.Editor
{
    internal class AssemblySelectionWindow : EditorWindow
    {
        private MultiColumnHeaderState m_MultiColumnHeaderState;
        private MultiSelectionTable m_MultiSelectionTable;
        private ProjectAuditorWindow m_ProjectAuditorWindow;
        private TreeViewState m_TreeViewState;

        public static AssemblySelectionWindow Open(float screenX, float screenY,
            ProjectAuditorWindow projectAuditorWindow, TreeViewSelection selection, string[] names)
        {
            var window = GetWindow<AssemblySelectionWindow>("Assemblies");
            window.position = new Rect(screenX, screenY, 400, 500);
            window.SetData(projectAuditorWindow, selection, names);
            window.Show();

            return window;
        }

        public static void CloseAll()
        {
            var window = GetWindow<AssemblySelectionWindow>("Assemblies");
            window.Close();
        }

        private void OnLostFocus()
        {
            Close();
        }

        private void OnDestroy()
        {
            m_ProjectAuditorWindow.SetAssemblySelection(m_MultiSelectionTable.GetTreeViewSelection());
        }

        public static bool IsOpen()
        {
            var windows = Resources.FindObjectsOfTypeAll(typeof(AssemblySelectionWindow));
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
                new MultiSelectionTable.HeaderData("Assembly", "Assembly Name", 350, 100, true, false),
                new MultiSelectionTable.HeaderData("Show", "Check to show this assembly in the analysis views", 40, 100,
                    false, false),
                new MultiSelectionTable.HeaderData("Group", "Assembly Group", 100, 100, true, false)
            };
            m_MultiColumnHeaderState = MultiSelectionTable.CreateDefaultMultiColumnHeaderState(headerData);

            var multiColumnHeader = new MultiColumnHeader(m_MultiColumnHeaderState);
            multiColumnHeader.SetSorting((int)MultiSelectionTable.MyColumns.ItemName, true);
            multiColumnHeader.ResizeToFit();
            m_MultiSelectionTable = new MultiSelectionTable(m_TreeViewState, multiColumnHeader, names, selection);
        }

        private void OnGUI()
        {
            EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true));
            var style = new GUIStyle(GUI.skin.label);
            style.alignment = TextAnchor.MiddleLeft;
            GUILayout.Label("Select Assembly : ", style);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Clear", GUILayout.Width(50))) m_MultiSelectionTable.ClearSelection();
            if (GUILayout.Button("Apply", GUILayout.Width(50)))
                m_ProjectAuditorWindow.SetAssemblySelection(m_MultiSelectionTable.GetTreeViewSelection());
            EditorGUILayout.EndHorizontal();

            if (m_MultiSelectionTable != null)
            {
                var r = EditorGUILayout.GetControlRect(GUILayout.ExpandHeight(true));
                m_MultiSelectionTable.OnGUI(r);
            }

            EditorGUILayout.EndVertical();
        }
    }
}
