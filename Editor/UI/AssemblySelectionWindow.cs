using UnityEngine;
using UnityEditor.IMGUI.Controls;
using UnityEditor;

namespace Unity.ProjectAuditor.Editor
{
    public class AssemblySelectionWindow : EditorWindow
    {
        ProjectAuditorWindow m_ProjectAuditorWindow;
        TreeViewState m_TreeViewState;
        MultiColumnHeaderState m_MultiColumnHeaderState;
        AssemblyTable m_AssemblyTable;

        static public AssemblySelectionWindow Open(float screenX, float screenY, ProjectAuditorWindow projectAuditorWindow, TreeViewSelection selection, string[] names)
        {
            AssemblySelectionWindow window = GetWindow<AssemblySelectionWindow>("Assemblies");
            window.position = new Rect(screenX, screenY, 400, 500);
            window.SetData(projectAuditorWindow, selection, names);
            window.Show();

            return window;
        }

        static public void CloseAll()
        {
            AssemblySelectionWindow window = GetWindow<AssemblySelectionWindow>("Assemblies");
            window.Close();
        }
        
        private void OnLostFocus()
        {
            Close();
        }
        
        private void OnDestroy()
        {
            m_ProjectAuditorWindow.SetAssemblySelection(m_AssemblyTable.GetAssemblySelection());
        }
        
        static public bool IsOpen()
        {
            UnityEngine.Object[] windows = Resources.FindObjectsOfTypeAll(typeof(AssemblySelectionWindow));
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

            m_MultiColumnHeaderState = AssemblyTable.CreateDefaultMultiColumnHeaderState(700);

            var multiColumnHeader = new MultiColumnHeader(m_MultiColumnHeaderState);
            multiColumnHeader.SetSorting((int)AssemblyTable.MyColumns.AssemblyName, true);
            multiColumnHeader.ResizeToFit();
            m_AssemblyTable = new AssemblyTable(m_TreeViewState, multiColumnHeader, names, selection);
        }
        
        void OnGUI()
        {
            EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true));
            GUIStyle style = new GUIStyle(GUI.skin.label);
            style.alignment = TextAnchor.MiddleLeft;
            GUILayout.Label("Select Assembly : ", style);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Clear", GUILayout.Width(50)))
            {
                m_AssemblyTable.ClearAssemblySelection();
            }
            if (GUILayout.Button("Apply", GUILayout.Width(50)))
            {
                m_ProjectAuditorWindow.SetAssemblySelection(m_AssemblyTable.GetAssemblySelection());
            }
            EditorGUILayout.EndHorizontal();

            if (m_AssemblyTable != null)
            {
                Rect r = EditorGUILayout.GetControlRect(GUILayout.ExpandHeight(true));
                m_AssemblyTable.OnGUI(r);
            }

            EditorGUILayout.EndVertical();
        }
    }
}
