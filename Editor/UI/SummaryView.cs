using UnityEditor;

namespace Unity.ProjectAuditor.Editor.UI
{
    public class SummaryView : AnalysisView
    {
        protected override void OnDrawInfo()
        {
            EditorGUILayout.LabelField("Select a View from the toolbar to start browsing the report", EditorStyles.textArea);
        }
    }
}
