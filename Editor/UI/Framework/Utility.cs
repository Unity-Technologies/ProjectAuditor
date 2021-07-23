using System;
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

        public static readonly GUIContent CopyToClipboard = new GUIContent("Copy to Clipboard");

        public static GUIContent InfoIcon
        {
            get
            {
#if UNITY_2018_3_OR_NEWER
                return EditorGUIUtility.TrIconContent(k_InfoIconName, "Info");
#else
                return new GUIContent(EditorGUIUtility.FindTexture(Utility.k_InfoIconName), "Info"), s_LabelStyle);
#endif
            }
        }


        public static GUIContent WarnIcon
        {
            get
            {
#if UNITY_2018_3_OR_NEWER
                return EditorGUIUtility.TrIconContent(k_WarnIconName, "Warning");
#else
                return new GUIContent(EditorGUIUtility.FindTexture(Utility.k_WarnIconName), "Warning"), s_LabelStyle);
#endif
            }
        }

        public static GUIContent ErrorIcon
        {
            get
            {
#if UNITY_2018_3_OR_NEWER
                return EditorGUIUtility.TrIconContent(k_ErrorIconName, "Error");
#else
                return new GUIContent(EditorGUIUtility.FindTexture(Utility.k_ErrorIconName), "Error"), s_LabelStyle);
#endif
            }
        }

        public class DropdownItem
        {
            public GUIContent Content;
            public GUIContent SelectionContent;
            public bool Enabled;
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
                        menu.AddItem(items[i].Content, i == selectionIndex, callback, i);
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
            var treeViewSelectionStyle = (GUIStyle) "TV Selection";
            var backgroundStyle = new GUIStyle(treeViewSelectionStyle);

            var treeViewLineStyle = (GUIStyle) "TV Line";
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

            return string.Join(", ", selectedStrings);
        }

        public static GUIContent GetIcon(string name)
        {
            return EditorGUIUtility.TrIconContent(ProjectAuditor.PackagePath + "/Editor/Icons/" + name + ".png");
        }

        public static GUIContent GetStatusWheel()
        {
            if (s_StatusWheel == null)
            {
                s_StatusWheel = new GUIContent[12];
                for (int i = 0; i < 12; i++)
                    s_StatusWheel[i] = EditorGUIUtility.IconContent("WaitSpin" + i.ToString("00"));

            }

            int frame = (int) Mathf.Repeat(Time.realtimeSinceStartup * 10, 11.99f);
            return s_StatusWheel[frame];
        }

        public static GUIContent GetTextContentWithAssetIcon(string displayName, string assetPath)
        {
#if UNITY_2018_3_OR_NEWER
            var icon = AssetDatabase.GetCachedIcon(assetPath);
            return EditorGUIUtility.TrTextContentWithIcon(displayName, assetPath, icon);
#else
            return new GUIContent(item.GetDisplayName(), issue.location.Path);
#endif
        }
    }
}
