using System.Linq;
using UnityEditor;

namespace Unity.ProjectAuditor.Editor.UI
{
    public class CodeView : AnalysisView
    {
        protected override void OnDrawInfo()
        {
            var compilerMessages = s_Report.GetIssues(IssueCategory.CodeCompilerMessages);
            var numCompilationErrors = compilerMessages.Count(i => i.severity == Rule.Severity.Error);
            if (numCompilationErrors > 0)
            {
                EditorGUILayout.LabelField("Code Analysis not available due to compilation errors");
            }
            else
            {
                EditorGUILayout.LabelField("- Use the Filters to reduce the number of reported issues");
                EditorGUILayout.LabelField("- Use the Mute button to mark an issue as false-positive");
            }
        }
    }
}
