using UnityEditor;
using UnityEngine;

namespace Unity.ProjectAuditor.Editor.UI
{
    internal static class SharedStyles
    {
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

        static GUIStyle s_TextArea;
        static GUIStyle s_TextFieldWarning;
    }

}
