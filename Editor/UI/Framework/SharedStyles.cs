using UnityEditor;
using UnityEngine;

namespace Unity.ProjectAuditor.Editor.UI.Framework
{
    public static class SharedStyles
    {
        static GUIStyle s_Foldout;
        static GUIStyle s_Label;
        static GUIStyle s_TextArea;
        static GUIStyle s_TextFieldWarning;

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


        public static GUIStyle TextArea
        {
            get
            {
                if (s_TextArea == null)
                    s_TextArea = new GUIStyle(EditorStyles.textArea);
                return s_TextArea;
            }
        }

        public static GUIStyle TextFieldWarning
        {
            get
            {
                if (s_TextFieldWarning == null)
                {
                    s_TextFieldWarning = new GUIStyle(EditorStyles.textField);
                    s_TextFieldWarning.normal.textColor = Color.yellow;
                }

                return s_TextFieldWarning;
            }
        }


#if UNITY_2018_1_OR_NEWER
        public static readonly GUIContent HelpButton = EditorGUIUtility.TrIconContent("_Help", "Open Manual (in a web browser)");
#else
        public static readonly GUIContent HelpButton = new GUIContent("?", "Open Manual (in a web browser)");
#endif

        public static GUIContent[] StatusWheel;
    }
}
