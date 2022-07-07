using Unity.ProjectAuditor.Editor.UI.Framework;
using Unity.ProjectAuditor.Editor.AssemblyUtils;
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
            if (m_Config.CompilationMode == CompilationMode.Editor) // TODO: check setting at time of analysis
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.HelpBox(k_NotAvailable, MessageType.Warning);
                EditorGUILayout.EndHorizontal();
            }
        }
    }
}
