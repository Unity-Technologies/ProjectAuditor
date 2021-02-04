using System;
using UnityEditor;
using UnityEngine;

namespace Unity.ProjectAuditor.Editor.UI
{
    public static class Utility
    {
        static class Styles
        {
            public static GUIStyle DropDownButton;
            public static GUIStyle Foldout;
        }

        public static bool BoldFoldout(bool toggle, GUIContent content)
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

        public static bool ButtonWithDropdownList(GUIContent content, string[] buttonNames, GenericMenu.MenuFunction2 callback, params GUILayoutOption[] options)
        {
            if (Styles.DropDownButton == null)
                Styles.DropDownButton = GUI.skin.FindStyle("DropDownButton");

            var rect = GUILayoutUtility.GetRect(content, Styles.DropDownButton, options);
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

            return GUI.Button(rect, content, Styles.DropDownButton);
        }
    }
}
