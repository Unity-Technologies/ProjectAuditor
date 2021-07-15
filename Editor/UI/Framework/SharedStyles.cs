using UnityEditor;
using UnityEngine;

namespace Unity.ProjectAuditor.Editor.UI.Framework
{
    public static class SharedStyles
    {
        static GUIStyle s_Foldout;
        static GUIStyle s_Label;
        static GUIStyle s_LinkLabel;
        static GUIStyle s_TextArea;

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

        public static GUIStyle Label
        {
            get
            {
                if (s_Label == null)
                    s_Label = new GUIStyle(EditorStyles.label);
                return s_Label;
            }
        }

        public static GUIStyle LinkLabel
        {
            get
            {
                if (s_LinkLabel == null)
                    s_LinkLabel = GetStyle("LinkLabel");
                return s_LinkLabel;
            }
        }

        public static GUIStyle TextArea
        {
            get
            {
                if (s_TextArea == null)
                    s_TextArea = new GUIStyle(EditorStyles.textArea);
                return s_TextArea;
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
    }
}
