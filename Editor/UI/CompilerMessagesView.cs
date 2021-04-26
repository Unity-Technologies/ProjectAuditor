using UnityEditor;
using UnityEngine;

namespace Unity.ProjectAuditor.Editor.UI
{
    class CompilerMessagesView : AnalysisView
    {
        const string k_Info = "This view shows the compiler warnings and errors.";
        const string k_NotAvailable = "This view is not available when 'AnalyzeEditorCode' is enabled.";

        protected override void OnDrawInfo()
        {
            EditorGUILayout.LabelField(k_Info);
            if (m_Config.AnalyzeEditorCode)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(k_NotAvailable, GUILayout.MaxWidth(380));
                EditorGUILayout.LabelField(Utility.WarnIcon);
                EditorGUILayout.EndHorizontal();
            }
        }
    }
}
