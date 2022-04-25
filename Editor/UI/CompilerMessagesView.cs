using Unity.ProjectAuditor.Editor.UI.Framework;
using Unity.ProjectAuditor.Editor.Utils;
using UnityEditor;
using UnityEngine;

namespace Unity.ProjectAuditor.Editor.UI
{
    class CompilerMessagesView : AnalysisView
    {
        const string k_Info = "This view shows the compiler error, warning and info messages.";
        const string k_NotAvailable = "This view is not available when 'CompilationMode' is set to 'CompilationMode.Editor'.";

        public CompilerMessagesView(ViewManager viewManager) : base(viewManager)
        {
        }

        protected override void OnDrawInfo()
        {
            EditorGUILayout.LabelField(k_Info);
            if (m_Config.CompilationMode == CompilationMode.Editor)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(k_NotAvailable, GUILayout.MaxWidth(380));
                EditorGUILayout.LabelField(Utility.GetSeverityIcon(Rule.Severity.Warning));
                EditorGUILayout.EndHorizontal();
            }
        }
    }
}
