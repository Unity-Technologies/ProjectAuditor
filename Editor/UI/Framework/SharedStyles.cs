using UnityEditor;
using UnityEngine;

namespace Unity.ProjectAuditor.Editor.UI.Framework
{
    internal static class SharedStyles
    {
        const int k_RowSize = 22;

        static GUIStyle s_Foldout;
        static GUIStyle s_BoldLabel;
        static GUIStyle s_IconLabel;
        static GUIStyle s_Label;
        static GUIStyle s_LabelWithRichtext;
        static GUIStyle s_LinkLabel;
        static GUIStyle s_TextArea;

        static GUIStyle s_LabelWithDynamicSize;
        static GUIStyle s_TextAreaWithDynamicSize;

        static GUIStyle s_TitleLabel;
        static GUIStyle s_LargeLabel;
        static GUIStyle s_WhiteLargeLabel;

        static GUIStyle s_Row;
        static GUIStyle s_RowAlternate;

        public static GUIStyle Foldout
        {
            get
            {
                if (s_Foldout == null)
                    s_Foldout = new GUIStyle(EditorStyles.foldout)
                    {
                        fontStyle = FontStyle.Bold
                    };
                return s_Foldout;
            }
        }

        public static GUIStyle BoldLabel
        {
            get
            {
                if (s_BoldLabel == null)
                    s_BoldLabel = new GUIStyle(EditorStyles.label)
                    {
                        fontStyle = FontStyle.Bold,
                        wordWrap = false
                    };
                return s_BoldLabel;
            }
        }

        public static GUIStyle IconLabel
        {
            get
            {
                if (s_IconLabel == null)
                    s_IconLabel = new GUIStyle(EditorStyles.label)
                    {
                        alignment = TextAnchor.MiddleCenter,
                        wordWrap = false
                    };
                return s_IconLabel;
            }
        }

        public static GUIStyle Label
        {
            get
            {
                if (s_Label == null)
                    s_Label = new GUIStyle(EditorStyles.label)
                    {
                        wordWrap = false
                    };
                return s_Label;
            }
        }

        public static GUIStyle LabelRichText
        {
            get
            {
                if (s_LabelWithRichtext == null)
                    s_LabelWithRichtext = new GUIStyle(EditorStyles.label)
                    {
                        richText = true
                    };
                return s_LabelWithRichtext;
            }
        }

        public static GUIStyle LinkLabel
        {
            get
            {
                if (s_LinkLabel == null)
                    s_LinkLabel = new GUIStyle(GetStyle("LinkLabel"))
                    {
                        alignment   = TextAnchor.MiddleLeft
                    };
                return s_LinkLabel;
            }
        }

        public static GUIStyle TextArea
        {
            get
            {
                if (s_TextArea == null)
                {
                    s_TextArea = new GUIStyle(EditorStyles.label);
                    s_TextArea.richText = true;
                    s_TextArea.wordWrap = true;
                    s_TextArea.alignment = TextAnchor.UpperLeft;
                }

                return s_TextArea;
            }
        }


        public static GUIStyle LabelWithDynamicSizeWithDynamicSize
        {
            get
            {
                if (s_LabelWithDynamicSize == null)
                    s_LabelWithDynamicSize = new GUIStyle(EditorStyles.label)
                    {
                        wordWrap = false
                    };
                return s_LabelWithDynamicSize;
            }
        }

        public static GUIStyle TextAreaWithDynamicSize
        {
            get
            {
                if (s_TextAreaWithDynamicSize == null)
                {
                    s_TextAreaWithDynamicSize = new GUIStyle(EditorStyles.label);
                    s_TextAreaWithDynamicSize.richText = true;
                    s_TextAreaWithDynamicSize.wordWrap = true;
                    s_TextAreaWithDynamicSize.alignment = TextAnchor.UpperLeft;
                }

                return s_TextAreaWithDynamicSize;
            }
        }

        public static GUIStyle TitleLabel
        {
            get
            {
                if (s_TitleLabel == null)
                {
                    s_TitleLabel = new GUIStyle(EditorStyles.boldLabel);
                    s_TitleLabel.fontSize = 26;
                    s_TitleLabel.fixedHeight = 34;
                }
                return s_TitleLabel;
            }
        }

        public static GUIStyle LargeLabel
        {
            get
            {
                if (s_LargeLabel == null)
                {
                    s_LargeLabel = new GUIStyle(EditorStyles.boldLabel);
                    s_LargeLabel.fontSize = 14;
                    s_LargeLabel.fixedHeight = 22;
                }
                return s_LargeLabel;
            }
        }

        public static GUIStyle WhiteLargeLabel
        {
            get
            {
                if (s_WhiteLargeLabel == null)
                {
                    s_WhiteLargeLabel = new GUIStyle(EditorStyles.boldLabel);
                    s_WhiteLargeLabel.fontSize = 14;
                    s_WhiteLargeLabel.fixedHeight = 22;
                    s_WhiteLargeLabel.normal.textColor = Color.white;
                    s_WhiteLargeLabel.hover.textColor = Color.white;
                }
                return s_WhiteLargeLabel;
            }
        }

        static GUIStyle GetStyle(string styleName)
        {
            var s = GUI.skin.FindStyle(styleName);
            if (s == null)
                s = EditorGUIUtility.GetBuiltinSkin(EditorSkin.Inspector).FindStyle(styleName);
            if (s == null)
            {
                Debug.LogError("Missing built-in guistyle " + styleName);
                s = new GUIStyle();
            }
            return s;
        }

        public static void SetFontDynamicSize(int fontSize)
        {
            LabelWithDynamicSizeWithDynamicSize.fontSize = fontSize;
            TextAreaWithDynamicSize.fontSize = fontSize;
        }

        public static GUIStyle Row
        {
            get
            {
                if (s_Row == null || s_Row.normal.background == null)
                {
                    s_Row = new GUIStyle(GUIStyle.none)
                    {
                        normal = {background = Utility.MakeColorTexture(new Color(0.22f, 0.22f, 0.22f, 1.0f))},
                        fixedHeight = k_RowSize
                    };
                }
                return s_Row;
            }
        }

        public static GUIStyle RowAlternate
        {
            get
            {
                if (s_RowAlternate == null || s_RowAlternate.normal.background == null)
                {
                    s_RowAlternate = new GUIStyle(GUIStyle.none)
                    {
                        normal = {background = Utility.MakeColorTexture(new Color(0.275f, 0.275f, 0.275f, 1.0f))},
                        fixedHeight = k_RowSize
                    };
                }
                return s_RowAlternate;
            }
        }
    }
}
