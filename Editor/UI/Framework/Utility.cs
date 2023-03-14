using System;
using Unity.ProjectAuditor.Editor.Diagnostic;
using Unity.ProjectAuditor.Editor.Utils;
using UnityEditor;
using UnityEngine;

namespace Unity.ProjectAuditor.Editor.UI.Framework
{
    public static class Utility
    {
        public enum IconType
        {
            Info,
            Warning,
            Error,

            Critical,
            Major,
            Moderate,
            Minor,

            Help,
            Refresh,
            Settings,

            StatusWheel,
            Hierarchy,
            ZoomTool,
            Fix,
            Download,
            Load,
            Save,
            Trash,
            View,
            WhiteCheckMark,
            GreenCheckMark,
        }

        // Log level
        static readonly string k_InfoIconName = "console.infoicon";
        static readonly string k_WarningIconName = "console.warnicon";
        static readonly string k_ErrorIconName = "console.erroricon";

        // Severity
        static readonly string k_CriticalIconName = "Critical";
        static readonly string k_MajorIconName = "Major";
        static readonly string k_ModerateIconName = "Moderate";
        static readonly string k_MinorIconName = "Minor";

        static readonly string k_HelpIconName = "_Help";
        static readonly string k_RefreshIconName = "Refresh";
        static readonly string k_SettingsIconName = "Settings";

        static readonly string k_WhiteCheckMarkIconName = "FilterSelectedOnly";
        static readonly string k_GreenCheckMarkIconName = "TestPassed";
        static readonly string k_HierarchyIconName = "UnityEditor.SceneHierarchyWindow";
        static readonly string k_ZoomToolIconName = "ViewToolZoom";
        static readonly string k_FixIconName = "Profiler.Custom"; // Only available in 2020+
        static readonly string k_DownloadIconName = "Download-Available"; // Only available in 2020+
        static readonly string k_LoadIconName = "Import";
        static readonly string k_SaveIconName = "SaveAs";
        static readonly string k_TrashIconName = "TreeEditor.Trash";
        static readonly string k_ViewIconName = "ViewToolOrbit";

        static Texture2D s_CriticalIcon;
        static Texture2D s_MajorIcon;
        static Texture2D s_ModerateIcon;
        static Texture2D s_MinorIcon;

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

        public static void DrawHelpButton(GUIContent content, string url)
        {
            if (GUILayout.Button(content, EditorStyles.toolbarButton, GUILayout.MaxWidth(25)))
            {
                Application.OpenURL(url);
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

        static string GetPlatformIconName(BuildTargetGroup buildTargetGroup)
        {
            string platformName;
            if (buildTargetGroup == BuildTargetGroup.Unknown)
                return "BuildSettings.Broadcom";

            switch (buildTargetGroup)
            {
                case BuildTargetGroup.WSA:
                    platformName = "Metro";
                    break;
                default:
                    platformName = buildTargetGroup.ToString();
                    break;
            }

            return $"BuildSettings.{platformName}.Small";
        }

        public static GUIContent GetPlatformIcon(BuildTargetGroup buildTargetGroup)
        {
            var iconName = GetPlatformIconName(buildTargetGroup);

            return EditorGUIUtility.IconContent(iconName);
        }

        public static GUIContent GetIcon(IconType iconType, string tooltip = null)
        {
            switch (iconType)
            {
                // log level icons
                case IconType.Info:
                    if (string.IsNullOrEmpty(tooltip))
                        tooltip = "Info";
                    return EditorGUIUtility.TrIconContent(k_InfoIconName, tooltip);
                case IconType.Warning:
                    if (string.IsNullOrEmpty(tooltip))
                        tooltip = "Warning";
                    return EditorGUIUtility.TrIconContent(k_WarningIconName, tooltip);
                case IconType.Error:
                    if (string.IsNullOrEmpty(tooltip))
                        tooltip = "Error";
                    return EditorGUIUtility.TrIconContent(k_ErrorIconName, tooltip);

                // severity icons
                case IconType.Critical:
                    if (string.IsNullOrEmpty(tooltip))
                        tooltip = "Critical";
                    if (s_CriticalIcon == null)
                        s_CriticalIcon = LoadIcon(k_CriticalIconName);
                    return EditorGUIUtility.TrIconContent(s_CriticalIcon, tooltip);
                case IconType.Major:
                    if (string.IsNullOrEmpty(tooltip))
                        tooltip = "Major";
                    if (s_MajorIcon == null)
                        s_MajorIcon = LoadIcon(k_MajorIconName);
                    return EditorGUIUtility.TrIconContent(s_MajorIcon, tooltip);
                case IconType.Moderate:
                    if (string.IsNullOrEmpty(tooltip))
                        tooltip = "Moderate";
                    if (s_ModerateIcon == null)
                        s_ModerateIcon = LoadIcon(k_ModerateIconName);
                    return EditorGUIUtility.TrIconContent(s_ModerateIcon, tooltip);
                case IconType.Minor:
                    if (string.IsNullOrEmpty(tooltip))
                        tooltip = "Minor";
                    if (s_MinorIcon == null)
                        s_MinorIcon = LoadIcon(k_MinorIconName);
                    return EditorGUIUtility.TrIconContent(s_MinorIcon, tooltip);

                case IconType.Hierarchy:
                    return EditorGUIUtility.TrIconContent(k_HierarchyIconName, tooltip);
                case IconType.ZoomTool:
                    return EditorGUIUtility.TrIconContent(k_ZoomToolIconName, tooltip);
                case IconType.Fix:
                    return EditorGUIUtility.TrIconContent(k_FixIconName, tooltip);
                case IconType.Download:
                    return EditorGUIUtility.TrIconContent(k_DownloadIconName, tooltip);
                case IconType.View:
                    return EditorGUIUtility.TrIconContent(k_ViewIconName, tooltip);
                case IconType.Help:
                    return EditorGUIUtility.TrIconContent(k_HelpIconName, tooltip);
                case IconType.Refresh:
                    return EditorGUIUtility.TrIconContent(k_RefreshIconName, tooltip);
                case IconType.Settings:
                    return EditorGUIUtility.TrIconContent(k_SettingsIconName, tooltip);
                case IconType.Load:
                    return EditorGUIUtility.TrIconContent(k_LoadIconName, tooltip);
                case IconType.Save:
                    return EditorGUIUtility.TrIconContent(k_SaveIconName, tooltip);
                case IconType.Trash:
                    return EditorGUIUtility.TrIconContent(k_TrashIconName, tooltip);
                case IconType.StatusWheel:
                    return GetStatusWheel();
                case IconType.WhiteCheckMark:
                    return EditorGUIUtility.TrIconContent(k_WhiteCheckMarkIconName, tooltip);
                case IconType.GreenCheckMark:
                    return EditorGUIUtility.TrIconContent(k_GreenCheckMarkIconName, tooltip);
            }

            return null;
        }

        public static GUIContent GetLogLevelIcon(Core.LogLevel logLevel, string tooltip = null)
        {
            switch (logLevel)
            {
                case Core.LogLevel.Info:
                    return GetIcon(IconType.Info, tooltip);
                case Core.LogLevel.Warning:
                    return GetIcon(IconType.Warning, tooltip);
                case Core.LogLevel.Error:
                    return GetIcon(IconType.Error, tooltip);
                default:
                    return GetIcon(IconType.Help, tooltip);
            }
        }

        public static GUIContent GetTextWithLogLevelIcon(string text, string tooltip, Severity severity)
        {
            switch (severity)
            {
                case Severity.Info:
                    return EditorGUIUtility.TrTextContentWithIcon(text, tooltip, MessageType.Info);
                case Severity.Warning:
                    return EditorGUIUtility.TrTextContentWithIcon(text, tooltip, MessageType.Warning);
                case Severity.Error:
                    return EditorGUIUtility.TrTextContentWithIcon(text, tooltip, MessageType.Error);
                default:
                    return EditorGUIUtility.TrTextContentWithIcon(text, tooltip, MessageType.None);
            }
        }

        public static GUIContent GetSeverityIcon(Severity severity, string tooltip = null)
        {
            switch (severity)
            {
                case Severity.Minor:
                    return GetIcon(IconType.Minor, tooltip);
                case Severity.Moderate:
                    return GetIcon(IconType.Moderate, tooltip);
                case Severity.Major:
                    return GetIcon(IconType.Major, tooltip);
                case Severity.Critical:
                    return GetIcon(IconType.Critical, tooltip);
                default:
                    return GetIcon(IconType.Help, tooltip);
            }
        }

        public static GUIContent GetSeverityIconWithText(Severity severity)
        {
            switch (severity)
            {
                case Severity.Minor:
                    if (s_MinorIcon == null)
                        s_MinorIcon = LoadIcon(k_MinorIconName);
                    return EditorGUIUtility.TrTextContentWithIcon("Minor", s_MinorIcon);
                case Severity.Moderate:
                    if (s_ModerateIcon == null)
                        s_ModerateIcon = LoadIcon(k_ModerateIconName);
                    return EditorGUIUtility.TrTextContentWithIcon("Moderate", s_ModerateIcon);
                case Severity.Major:
                    if (s_MajorIcon == null)
                        s_MajorIcon = LoadIcon(k_MajorIconName);
                    return EditorGUIUtility.TrTextContentWithIcon("Major", s_MajorIcon);
                case Severity.Critical:
                    if (s_CriticalIcon == null)
                        s_CriticalIcon = LoadIcon(k_CriticalIconName);
                    return EditorGUIUtility.TrTextContentWithIcon("Critical", s_CriticalIcon);
                default:
                    return EditorGUIUtility.TrTextContentWithIcon("Unknown", MessageType.None);
            }
        }

        static GUIContent GetStatusWheel()
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

        static Texture2D LoadIcon(string iconName)
        {
            return AssetDatabase.LoadAssetAtPath<Texture2D>($"{ProjectAuditor.s_PackagePath}/Editor/Icons/{iconName}.png");
        }

        public static Texture2D MakeColorTexture(Color col)
        {
            var pix = new Color[1];
            pix[0] = col;

            var result = new Texture2D(1, 1);
            result.SetPixels(pix);
            result.Apply();

            return result;
        }
    }
}
