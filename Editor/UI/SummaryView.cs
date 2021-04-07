using UnityEditor;

namespace Unity.ProjectAuditor.Editor.UI
{
    public class SummaryView : AnalysisView
    {
        protected override void OnDrawInfo()
        {
            EditorGUILayout.LabelField(ProjectAuditorWindow.Instructions, EditorStyles.textArea);
        }
    }
}
