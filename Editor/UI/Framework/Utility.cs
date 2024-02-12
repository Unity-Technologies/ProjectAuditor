using Unity.ProjectAuditor.Editor.Utils;
using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;

namespace Unity.ProjectAuditor.Editor.UI.Framework
{
    internal static class Utility
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
            Ignored,

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
            CopyToClipboard,
            AdditionalAnalysis,
            FoldoutExpanded,
            FoldoutFolded
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
        static readonly string k_IgnoredIconName = "Ignored";

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
        static readonly string k_DisplayedIgnoredIssuesIconName = "animationvisibilitytoggleon";
        static readonly string k_HiddenIgnoredIssuesIconName = "animationvisibilitytoggleoff";
        static readonly string k_IgnoredIssuesLabel = " Ignored Issues";
        static readonly string k_CopyToClipboardIconName = "CopyToClipboard";
        static readonly string k_AdditionalAnalysisIconName = "AdditionalAnalysis";
        static readonly string k_FoldoutExpandedIconName = "ClassicFoldoutArrow-Open";
        static readonly string k_FoldoutFoldedIconName = "ClassicFoldoutArrow-Close";

        static Texture2D s_CriticalIcon;
        static Texture2D s_MajorIcon;
        static Texture2D s_ModerateIcon;
        static Texture2D s_MinorIcon;
        static Texture2D s_IgnoredIcon;

        static Texture2D s_CopyToClipboardIcon;
        static Texture2D s_AdditionalAnalysisIcon;
        static Texture2D s_FoldoutExpandedIcon;
        static Texture2D s_FoldoutFoldedIcon;

        static GUIContent[] s_StatusWheel;

        static byte[] s_LetterWidths;
        static GUIStyle s_Style;
        static GUIContent s_GUIContent;

        public static readonly GUIContent ClearSelection = new GUIContent("Clear Selection");
        public static readonly GUIContent CopyToClipboard = new GUIContent("Copy to Clipboard");
        public static readonly GUIContent OpenIssue = new GUIContent("Open Issue");
        public static readonly GUIContent OpenScriptReference = new GUIContent("Open Script Reference");

        internal class DropdownItem
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
            var treeViewSelectionStyle = (GUIStyle)"TV Selection";
            var backgroundStyle = new GUIStyle(treeViewSelectionStyle);

            var treeViewLineStyle = (GUIStyle)"TV Line";
            var textStyle = new GUIStyle(treeViewLineStyle);

            var content = new GUIContent(text, text);
            var size = textStyle.CalcSize(content);
            var rect = EditorGUILayout.GetControlRect(GUILayout.MaxWidth(size.x), GUILayout.Height(size.y));
            if (Event.current.type == EventType.Repaint)
            {
                backgroundStyle.Draw(rect, false, false, true, true);
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

        public static GUIContent GetPlatformIconWithName(BuildTargetGroup buildTargetGroup)
        {
            var iconName = GetPlatformIconName(buildTargetGroup);
            return EditorGUIUtility.TrTextContentWithIcon(buildTargetGroup.ToString(), iconName);
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

                // Severity icons
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
                case IconType.Ignored:
                    if (string.IsNullOrEmpty(tooltip))
                        tooltip = "Ignored";
                    if (s_IgnoredIcon == null)
                        s_IgnoredIcon = LoadIcon(k_IgnoredIconName);
                    return EditorGUIUtility.TrIconContent(s_IgnoredIcon, tooltip);

                case IconType.CopyToClipboard:
                    if (string.IsNullOrEmpty(tooltip))
                        tooltip = "Copy to Clipboard";
                    if (s_CopyToClipboardIcon == null)
                        s_CopyToClipboardIcon = LoadIcon(k_CopyToClipboardIconName);
                    return EditorGUIUtility.TrIconContent(s_CopyToClipboardIcon, tooltip);
                case IconType.AdditionalAnalysis:
                    if (string.IsNullOrEmpty(tooltip))
                        tooltip = "Not Analyzed";
                    if (s_AdditionalAnalysisIcon == null)
                        s_AdditionalAnalysisIcon = LoadIcon(k_AdditionalAnalysisIconName);
                    return EditorGUIUtility.TrIconContent(s_AdditionalAnalysisIcon, tooltip);
                case IconType.FoldoutExpanded:
                    if (s_FoldoutExpandedIcon == null)
                        s_FoldoutExpandedIcon = LoadIcon(k_FoldoutExpandedIconName);
                    return EditorGUIUtility.TrIconContent(s_FoldoutExpandedIcon);
                case IconType.FoldoutFolded:
                    if (s_FoldoutFoldedIcon == null)
                        s_FoldoutFoldedIcon = LoadIcon(k_FoldoutFoldedIconName);
                    return EditorGUIUtility.TrIconContent(s_FoldoutFoldedIcon);

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

        public static GUIContent GetIconWithText(IconType iconType, string displayName, string tooltip = null)
        {
            switch (iconType)
            {
                case IconType.Refresh:
                    return EditorGUIUtility.TrTextContentWithIcon(displayName, tooltip, k_RefreshIconName);
            }

            return null;
        }

        public static GUIContent GetLogLevelIcon(LogLevel logLevel, string tooltip = null)
        {
            switch (logLevel)
            {
                case LogLevel.Info:
                    return GetIcon(IconType.Info, tooltip);
                case LogLevel.Warning:
                    return GetIcon(IconType.Warning, tooltip);
                case LogLevel.Error:
                    return GetIcon(IconType.Error, tooltip);
                default:
                    return GetIcon(IconType.Help, tooltip);
            }
        }

        public static GUIContent GetSeverityIcon(Severity severity)
        {
            switch (severity)
            {
                case Severity.Critical:
                    return GetIcon(IconType.Critical);
                case Severity.Major:
                    return GetIcon(IconType.Major);
                case Severity.Moderate:
                    return GetIcon(IconType.Moderate);
                default:
                    return GetIcon(IconType.Minor);
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

        public static GUIContent GetSeverityIconWithCustomText(Severity severity, string text)
        {
            switch (severity)
            {
                case Severity.Minor:
                    if (s_MinorIcon == null)
                        s_MinorIcon = LoadIcon(k_MinorIconName);
                    return EditorGUIUtility.TrTextContentWithIcon(text, s_MinorIcon);
                case Severity.Moderate:
                    if (s_ModerateIcon == null)
                        s_ModerateIcon = LoadIcon(k_ModerateIconName);
                    return EditorGUIUtility.TrTextContentWithIcon(text, s_ModerateIcon);
                case Severity.Major:
                    if (s_MajorIcon == null)
                        s_MajorIcon = LoadIcon(k_MajorIconName);
                    return EditorGUIUtility.TrTextContentWithIcon(text, s_MajorIcon);
                case Severity.Critical:
                    if (s_CriticalIcon == null)
                        s_CriticalIcon = LoadIcon(k_CriticalIconName);
                    return EditorGUIUtility.TrTextContentWithIcon(text, s_CriticalIcon);
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

        internal static GUIContent GetDisplayIgnoredIssuesIconWithLabel()
        {
            var guiContent = EditorGUIUtility.TrIconContent(k_DisplayedIgnoredIssuesIconName);
            guiContent.text = k_IgnoredIssuesLabel;
            return guiContent;
        }

        internal static GUIContent GetHiddenIgnoredIssuesIconWithLabel()
        {
            var guiContent = EditorGUIUtility.TrIconContent(k_HiddenIgnoredIssuesIconName);
            guiContent.text = k_IgnoredIssuesLabel;
            return guiContent;
        }

        static Texture2D LoadIcon(string iconName)
        {
            return AssetDatabase.LoadAssetAtPath<Texture2D>($"{ProjectAuditorPackage.Path}/Editor/Icons/{iconName}.png");
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

        // A quick and dirty way to get a rough width of a string, in comparison to other strings that also get passed to this method.
        // Used to find the widest string in a column. Pass that string to GetWidth_SlowButAccurate to get an actual width that includes kerning.
        public static float EstimateWidth(string text)
        {
            if (string.IsNullOrEmpty(text))
                return 0;

            Profiler.BeginSample("Utility.EstimateWidth");

            if (s_LetterWidths == null)
                s_LetterWidths = new byte[256];

            var style = GUI.skin.box;
            int totalWidth = 0;
            int len = text.Length;
            for (int i = 0; i < len; ++i)
            {
                var currChar = text[i];
                // Yes, we are crunching a 16-bit Unicode character down to a single byte, and will likely end up with
                // the wrong widths for non-English characters as a result. Why? Because the error will probably be
                // comparatively small, and because we want s_LetterWidths to fit into a cache-friendly 64 bytes rather than
                // a whole 16KB. We're in an extremely hot code path here, and speed is more important than accuracy.
                var charByte = (byte)currChar;
                byte charWidth = s_LetterWidths[charByte];
                if (charWidth == 0)
                {
                    var content = new GUIContent(currChar.ToString());
                    charWidth = (byte)((int)style.CalcSize(content).x);
                    s_LetterWidths[charByte] = charWidth;
                }

                totalWidth += charWidth;
            }

            Profiler.EndSample();
            return totalWidth;
        }

        public static float GetWidth_SlowButAccurate(string text, int fontSize)
        {
            Profiler.BeginSample("Utility.GetWidth_SlowButAccurate");

            if (s_Style == null)
                s_Style = EditorStyles.label;

            if (s_GUIContent == null)
                s_GUIContent = new GUIContent();

            s_Style.fontSize = fontSize;
            s_GUIContent.text = text;
            var width = s_Style.CalcSize(s_GUIContent).x;

            Profiler.EndSample();

            return width;
        }
    }
}
