using UnityEditor;

namespace Unity.ProjectAuditor.Editor.UI
{
    public class SummaryView : AnalysisView
    {
        static ProjectReport m_Report;

        public static void SetReport(ProjectReport report)
        {
            m_Report = report;
        }

        protected override void OnDrawInfo()
        {
            if (m_Report != null)
            {
                EditorGUILayout.LabelField("Analysis overview:");
                EditorGUILayout.LabelField("- Code Issues: " + m_Report.GetIssues(IssueCategory.Code).Length);
                EditorGUILayout.LabelField("- Settings Issues: " + m_Report.GetIssues(IssueCategory.ProjectSettings).Length);
                EditorGUILayout.LabelField("- Assets in Resources folders: " + m_Report.GetIssues(IssueCategory.Assets).Length);
                EditorGUILayout.LabelField("- Shaders in the project: " + m_Report.GetIssues(IssueCategory.Shaders).Length);

                EditorGUILayout.Space();
            }

            EditorGUILayout.LabelField("Select a View from the toolbar to start browsing the report");
        }
    }
}
