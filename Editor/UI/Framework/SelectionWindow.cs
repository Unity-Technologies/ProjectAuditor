using System;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace Unity.ProjectAuditor.Editor.UI.Framework
{
    internal abstract class SelectionWindow : EditorWindow
    {
        protected MultiSelectionTable m_SelectionTable;
        protected MultiColumnHeaderState m_MultiColumnHeaderState;
        protected TreeViewState m_TreeViewState;
        protected bool m_RequestClose;
        protected Action<TreeViewSelection> m_OnSelection;

        protected abstract void CreateTable(TreeViewSelection selection, string[] names);

        public static T Open<T>(string title, float screenX, float screenY,
            TreeViewSelection selection, string[] names, Action<TreeViewSelection> onSelection) where T : SelectionWindow
        {
            var window = GetWindow<T>(title);
            window.position = new Rect(screenX, screenY, 400, 500);
            window.SetData(selection, names, onSelection);
            window.Show();

            return window;
        }

        public static void CloseAll<T>() where T : SelectionWindow
        {
            var window = GetWindow<T>();
            window.Close();
        }

        public static bool IsOpen<T>() where T : SelectionWindow
        {
            var windows = Resources.FindObjectsOfTypeAll(typeof(T));
            if (windows != null && windows.Length > 0)
                return true;

            return false;
        }

        void OnEnable()
        {
            m_RequestClose = false;
        }

        void OnDestroy()
        {
            ApplySelection();
        }

        void OnGUI()
        {
            EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true));
            var style = new GUIStyle(GUI.skin.label);
            style.alignment = TextAnchor.MiddleLeft;
            GUILayout.Label("Select : ", style);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Clear", GUILayout.Width(50)))
                m_SelectionTable.ClearSelection();
            if (GUILayout.Button("Apply", GUILayout.Width(50)))
            {
                ApplySelection();
            }

            EditorGUILayout.EndHorizontal();

            if (m_SelectionTable != null)
            {
                var r = EditorGUILayout.GetControlRect(GUILayout.ExpandHeight(true));
                m_SelectionTable.OnGUI(r);
            }

            EditorGUILayout.EndVertical();
        }

        void OnLostFocus()
        {
            m_RequestClose = true;
        }

        void Update()
        {
            if (m_RequestClose)
                Close();
        }

        protected void ApplySelection()
        {
            m_OnSelection?.Invoke(m_SelectionTable.GetTreeViewSelection());
        }

        protected void SetData(TreeViewSelection selection, string[] names, Action<TreeViewSelection> onSelection)
        {
            CreateTable(selection, names);

            m_OnSelection = onSelection;
        }
    }
}
