using UnityEngine;
using UnityEditor.IMGUI.Controls;
using UnityEditor;

namespace Unity.ProjectAuditor.Editor
{
    public class AreaSelectionWindow : EditorWindow
    {
        ProjectAuditorWindow m_ProjectAuditorWindow;
        TreeViewState m_TreeViewState;
        MultiColumnHeaderState m_MultiColumnHeaderState;
        MultiSelectionTable m_AreaTable;

        static public AreaSelectionWindow Open(float screenX, float screenY, ProjectAuditorWindow projectAuditorWindow, TreeViewSelection selection, string[] names)
        {
            AreaSelectionWindow window = GetWindow<AreaSelectionWindow>("Areas");
            window.position = new Rect(screenX, screenY, 400, 500);
            window.SetData(projectAuditorWindow, selection, names);
            window.Show();

            return window;
        }

        static public void CloseAll()
        {
            AreaSelectionWindow window = GetWindow<AreaSelectionWindow>("Areas");
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
        
        static public bool IsOpen()
        {
            UnityEngine.Object[] windows = Resources.FindObjectsOfTypeAll(typeof(AreaSelectionWindow));
            if (windows != null && windows.Length > 0)
                return true;

            return false;
        }

        void SetData(ProjectAuditorWindow projectAuditorWindow, TreeViewSelection selection, string[] names)
        {
            m_ProjectAuditorWindow = projectAuditorWindow;
            CreateTable(projectAuditorWindow, selection, names);
        }
        
        void CreateTable(ProjectAuditorWindow projectAuditorWindow, TreeViewSelection selection, string[] names)
        {
            if (m_TreeViewState == null)
                m_TreeViewState = new TreeViewState();

            MultiSelectionTable.HeaderData[] headerData = new MultiSelectionTable.HeaderData[]
            {
                new MultiSelectionTable.HeaderData("Area", "Area Name", 350, 100, true, false),
                new MultiSelectionTable.HeaderData("Show", "Check to show issues affecting this area in the analysis views", 40, 100, false, false),
                new MultiSelectionTable.HeaderData("Group", "Group", 100, 100, true, false),

            };
            m_MultiColumnHeaderState = MultiSelectionTable.CreateDefaultMultiColumnHeaderState(headerData);

            var multiColumnHeader = new MultiColumnHeader(m_MultiColumnHeaderState);
            multiColumnHeader.SetSorting((int)MultiSelectionTable.MyColumns.ItemName, true);
            multiColumnHeader.ResizeToFit();
            m_AreaTable = new MultiSelectionTable(m_TreeViewState, multiColumnHeader, names, selection);
        }
        
        void OnGUI()
        {
            EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true));
            GUIStyle style = new GUIStyle(GUI.skin.label);
            style.alignment = TextAnchor.MiddleLeft;
            GUILayout.Label("Select Area : ", style);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Clear", GUILayout.Width(50)))
            {
                m_AreaTable.ClearSelection();
            }
            if (GUILayout.Button("Apply", GUILayout.Width(50)))
            {
                m_ProjectAuditorWindow.SetAreaSelection(m_AreaTable.GetTreeViewSelection());
            }
            EditorGUILayout.EndHorizontal();

            if (m_AreaTable != null)
            {
                Rect r = EditorGUILayout.GetControlRect(GUILayout.ExpandHeight(true));
                m_AreaTable.OnGUI(r);
            }

            EditorGUILayout.EndVertical();
        }
    }
}
