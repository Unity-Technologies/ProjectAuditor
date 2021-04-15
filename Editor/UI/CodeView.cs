using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Unity.ProjectAuditor.Editor.UI
{
    public class CodeView : AnalysisView
    {
        protected override void OnDrawInfo()
        {
            if (NumCompilationErrors() > 0)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(Utility.ErrorIcon, GUILayout.MaxWidth(36));
                EditorGUILayout.LabelField(new GUIContent("Code Analysis is not available due to compilation errors"));
                EditorGUILayout.EndHorizontal();
            }
            else
            {
                EditorGUILayout.LabelField("- Use the Filters to reduce the number of reported issues");
                EditorGUILayout.LabelField("- Use the Mute button to mark an issue as false-positive");
            }
        }

        public override bool IsValid()
        {
            return (NumCompilationErrors() == 0) && base.IsValid();
        }

        static int NumCompilationErrors()
        {
            var compilerMessages = s_Report.GetIssues(IssueCategory.CodeCompilerMessages);
            return compilerMessages.Count(i => i.severity == Rule.Severity.Error);
        }
    }
}
