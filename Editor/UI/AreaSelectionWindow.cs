using System;
using System.Collections.Generic;
using Unity.ProjectAuditor.Editor.UI.Framework;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace Unity.ProjectAuditor.Editor.UI
{
    class AreaSelectionWindow : EditorWindow
    {
        MultiSelectionTable m_AreaTable;
        MultiColumnHeaderState m_MultiColumnHeaderState;
        ProjectAuditorWindow m_ProjectAuditorWindow;
        TreeViewState m_TreeViewState;
        bool m_RequestClose;

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

        void OnEnable()
        {
            m_RequestClose = false;
        }

        void OnDestroy()
        {
            ApplySelection();
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

        public static bool IsOpen()
        {
            var windows = Resources.FindObjectsOfTypeAll(typeof(AreaSelectionWindow));
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

            MultiSelectionTable.HeaderData[] headerData =
            {
                new MultiSelectionTable.HeaderData("Area", "Area Name", 350, 100, true, false),
                new MultiSelectionTable.HeaderData("Show",
                    "Check to show issues affecting this area in the analysis views", 40, 100, false, false),
                new MultiSelectionTable.HeaderData("Group", "Group", 100, 100, true, false)
            };
            m_MultiColumnHeaderState = MultiSelectionTable.CreateDefaultMultiColumnHeaderState(headerData);

            var multiColumnHeader = new MultiColumnHeader(m_MultiColumnHeaderState);
            multiColumnHeader.SetSorting((int)MultiSelectionTable.Column.ItemName, true);
            multiColumnHeader.ResizeToFit();
            m_AreaTable = new MultiSelectionTable(m_TreeViewState, multiColumnHeader, names, selection);
        }

        void ApplySelection()
        {
            var analytic = ProjectAuditorAnalytics.BeginAnalytic();
            m_ProjectAuditorWindow.SetAreaSelection(m_AreaTable.GetTreeViewSelection());

            var payload = new Dictionary<string, string>();
            payload["areas"] = m_ProjectAuditorWindow.GetSelectedAreasSummary();
            ProjectAuditorAnalytics.SendEventWithKeyValues(ProjectAuditorAnalytics.UIButton.AreaSelectApply, analytic, payload);
        }

        void OnGUI()
        {
            EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true));
            var style = new GUIStyle(GUI.skin.label);
            style.alignment = TextAnchor.MiddleLeft;
            GUILayout.Label("Select Area : ", style);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Clear", GUILayout.Width(50)))
                m_AreaTable.ClearSelection();
            if (GUILayout.Button("Apply", GUILayout.Width(50)))
            {
                ApplySelection();
            }

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
