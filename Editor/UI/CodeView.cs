using System.Linq;
using Unity.ProjectAuditor.Editor.UI.Framework;
using UnityEditor;
using UnityEngine;

namespace Unity.ProjectAuditor.Editor.UI
{
    public class CodeView : AnalysisView
    {
        protected override void OnDrawInfo()
        {
            EditorGUILayout.LabelField("- Use the Filters to reduce the number of reported issues");
            EditorGUILayout.LabelField("- Use the Mute button to mark an issue as false-positive");

            if (NumCompilationErrors() > 0)
            {
                EditorGUILayout.Space();
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(Utility.ErrorIcon, GUILayout.MaxWidth(36));
                EditorGUILayout.LabelField(new GUIContent("Code Analysis is incomplete due to compilation errors"), GUILayout.Width(330), GUILayout.ExpandWidth(false));
                if (GUILayout.Button("View", EditorStyles.miniButton, GUILayout.Width(50)))
                    OnChangeView(IssueCategory.CodeCompilerMessage);
                EditorGUILayout.EndHorizontal();
            }
        }

        static int NumCompilationErrors()
        {
            var compilerMessages = s_Report.GetIssues(IssueCategory.CodeCompilerMessage);
            return compilerMessages.Count(i => i.severity == Rule.Severity.Error);
        }
    }
}
