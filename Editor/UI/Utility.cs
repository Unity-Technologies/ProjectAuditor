using System;
using UnityEditor;
using UnityEngine;

namespace Unity.ProjectAuditor.Editor.UI
{
    static class Utility
    {
        static class Styles
        {
            public static GUIStyle Foldout;
        }

        internal static bool BoldFoldout(bool toggle, GUIContent content)
        {
            if (Styles.Foldout == null)
            {
                Styles.Foldout = new GUIStyle(EditorStyles.foldout)
                {
                    fontStyle = FontStyle.Bold
                };
            }
            return EditorGUILayout.Foldout(toggle, content, Styles.Foldout);
        }

        internal static void ToolbarDropdownList(GUIContent content, string[] buttonNames, string activeSelection, GenericMenu.MenuFunction2 callback, params GUILayoutOption[] options)
        {
            var r = GUILayoutUtility.GetRect(content, EditorStyles.toolbarButton, GUILayout.Width(115f));
            if (EditorGUI.DropdownButton(r, content, FocusType.Passive, EditorStyles.toolbarDropDown))
            {
                var menu = new GenericMenu();

                for (var i = 0; i != buttonNames.Length; i++)
                    menu.AddItem(new GUIContent(buttonNames[i]), buttonNames[i] == activeSelection, callback, i);
                menu.DropDown(r);
            }
        }

        internal static bool ToolbarButtonWithDropdownList(GUIContent content, string[] buttonNames, GenericMenu.MenuFunction2 callback, params GUILayoutOption[] options)
        {
            var rect = GUILayoutUtility.GetRect(content, EditorStyles.toolbarDropDown, options);
            var dropDownRect = rect;

            const float kDropDownButtonWidth = 20f;
            dropDownRect.xMin = dropDownRect.xMax - kDropDownButtonWidth;

            if (Event.current.type == EventType.MouseDown && dropDownRect.Contains(Event.current.mousePosition))
            {
                var menu = new GenericMenu();
                for (var i = 0; i != buttonNames.Length; i++)
                    menu.AddItem(new GUIContent(buttonNames[i]), false, callback, i);

                menu.DropDown(rect);
                Event.current.Use();

                return false;
            }

            return GUI.Button(rect, content, EditorStyles.toolbarDropDown);
        }

        internal static void DrawSelectedText(string text)
        {
#if UNITY_2019_1_OR_NEWER
            var treeViewSelectionStyle = (GUIStyle)"TV Selection";
            var backgroundStyle = new GUIStyle(treeViewSelectionStyle);

            var treeViewLineStyle = (GUIStyle)"TV Line";
            var textStyle = new GUIStyle(treeViewLineStyle);
#else
            var textStyle = GUI.skin.label;
#endif

            var content = new GUIContent(text, text);
            var size = textStyle.CalcSize(content);
            var rect = EditorGUILayout.GetControlRect(GUILayout.MaxWidth(size.x), GUILayout.Height(size.y));
            if (Event.current.type == EventType.Repaint)
            {
#if UNITY_2019_1_OR_NEWER
                backgroundStyle.Draw(rect, false, false, true, true);
#endif
                GUI.Label(rect, content, textStyle);
            }
        }

        internal static string GetTreeViewSelectedSummary(TreeViewSelection selection, string[] names)
        {
            var selectedStrings = selection.GetSelectedStrings(names, true);
            var numStrings = selectedStrings.Length;

            if (numStrings == 0)
                return "None";

            if (numStrings == 1)
                return selectedStrings[0];

            return string.Join(", ", selectedStrings);
        }
    }
}
