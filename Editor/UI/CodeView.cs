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
        }
    }
}
