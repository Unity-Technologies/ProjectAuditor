using System;
using Unity.ProjectAuditor.Editor.Utils;
using UnityEditor;
using UnityEngine;

namespace Unity.ProjectAuditor.Editor.UI.Framework
{
    public static class Utility
    {
        static readonly string k_InfoIconName = "console.infoicon";
        static readonly string k_WarnIconName = "console.warnicon";
        static readonly string k_ErrorIconName = "console.erroricon";

        static GUIContent[] s_StatusWheel;

        public static readonly GUIContent ClearSelection = new GUIContent("Clear Selection");
        public static readonly GUIContent CopyToClipboard = new GUIContent("Copy to Clipboard");
        public static readonly GUIContent OpenIssue = new GUIContent("Open Issue");
        public static readonly GUIContent OpenScriptReference = new GUIContent("Open Script Reference");

        public class DropdownItem
        {
            public GUIContent Content;
            public GUIContent SelectionContent;
            public bool Enabled;
            public object UserData;
        }

        public static bool BoldFoldout(bool toggle, GUIContent content)
        {
            return EditorGUILayout.Foldout(toggle, content, SharedStyles.Foldout);
        }

        public static void ToolbarDropdownList(DropdownItem[] items, int selectionIndex,
            GenericMenu.MenuFunction2 callback, params GUILayoutOption[] options)
        {
            var selectionContent = items[selectionIndex].SelectionContent;
            var r = GUILayoutUtility.GetRect(selectionContent, EditorStyles.toolbarButton, options);
            if (EditorGUI.DropdownButton(r, selectionContent, FocusType.Passive, EditorStyles.toolbarDropDown))
            {
                var menu = new GenericMenu();

                for (var i = 0; i != items.Length; i++)
                    if (items[i].Enabled)
                        menu.AddItem(items[i].Content, i == selectionIndex, callback, items[i].UserData);
                    else
                        menu.AddDisabledItem(items[i].Content);
                menu.DropDown(r);
            }
        }

        internal static bool ToolbarButtonWithDropdownList(GUIContent content, string[] buttonNames,
            GenericMenu.MenuFunction2 callback, params GUILayoutOption[] options)
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

        public static void DrawHelpButton(GUIContent content, string page)
        {
            if (GUILayout.Button(content, EditorStyles.toolbarButton, GUILayout.MaxWidth(25)))
            {
                Application.OpenURL(Documentation.baseURL + ProjectAuditor.PackageVersion + Documentation.subURL + page + Documentation.endURL);
            }
        }

        public static void DrawSelectedText(string text)
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

        public static string GetTreeViewSelectedSummary(TreeViewSelection selection, string[] names)
        {
            var selectedStrings = selection.GetSelectedStrings(names, true);
            var numStrings = selectedStrings.Length;

            if (numStrings == 0)
                return "None";

            if (numStrings == 1)
                return selectedStrings[0];

            return Formatting.CombineStrings(selectedStrings);
        }

        public static GUIContent GetIcon(string name)
        {
            return EditorGUIUtility.TrIconContent(ProjectAuditor.PackagePath + "/Editor/Icons/" + name + ".png");
        }

        public static GUIContent GetSeverityIcon(Rule.Severity severity, string tooltip = null)
        {
            string iconName;
            switch (severity)
            {
                case Rule.Severity.Info:
                    iconName = k_InfoIconName;
                    if (string.IsNullOrEmpty(tooltip))
                        tooltip = "Info";
                    break;
                case Rule.Severity.Warning:
                    iconName = k_WarnIconName;
                    if (string.IsNullOrEmpty(tooltip))
                        tooltip = "Warning";
                    break;
                case Rule.Severity.Error:
                    iconName = k_ErrorIconName;
                    if (string.IsNullOrEmpty(tooltip))
                        tooltip = "Error";
                    break;
                default:
                    return null;
            }

#if UNITY_2019_3_OR_NEWER
            return EditorGUIUtility.TrIconContent(iconName, tooltip);
#else
            return new GUIContent(EditorGUIUtility.FindTexture(iconName), tooltip);
#endif
        }

        public static GUIContent GetTextWithSeverityIcon(string text, string tooltip, Rule.Severity severity)
        {
            switch (severity)
            {
                case Rule.Severity.Info:
                    return EditorGUIUtility.TrTextContentWithIcon(text, tooltip, MessageType.Info);
                case Rule.Severity.Warning:
                    return EditorGUIUtility.TrTextContentWithIcon(text, tooltip, MessageType.Warning);
                case Rule.Severity.Error:
                    return EditorGUIUtility.TrTextContentWithIcon(text, tooltip, MessageType.Error);
                default:
                    return EditorGUIUtility.TrTextContentWithIcon(text, tooltip, MessageType.None);
            }
        }

        public static GUIContent GetStatusWheel()
        {
            if (s_StatusWheel == null)
            {
                s_StatusWheel = new GUIContent[12];
                for (int i = 0; i < 12; i++)
                    s_StatusWheel[i] = EditorGUIUtility.IconContent("WaitSpin" + i.ToString("00"));
            }

            int frame = (int)Mathf.Repeat(Time.realtimeSinceStartup * 10, 11.99f);
            return s_StatusWheel[frame];
        }

        public static GUIContent GetTextContentWithAssetIcon(string displayName, string assetPath)
        {
            var icon = AssetDatabase.GetCachedIcon(assetPath);
            return EditorGUIUtility.TrTextContentWithIcon(displayName, assetPath, icon);
        }
    }
}
