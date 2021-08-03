using System.Collections.Generic;
using System.Linq;
using Unity.ProjectAuditor.Editor.UI.Framework;
using UnityEditor;
using UnityEngine;

namespace Unity.ProjectAuditor.Editor.UI
{
    public class CodeView : AnalysisView
    {
        int m_NumCompilerErrors = 0;

        public CodeView(ViewManager viewManager) : base(viewManager)
        {
        }

        public override void AddIssues(IEnumerable<ProjectIssue> allIssues)
        {
            base.AddIssues(allIssues);

            var compilerMessages = allIssues.Where(i => i.category == IssueCategory.CodeCompilerMessage);
            m_NumCompilerErrors += compilerMessages.Count(i => i.severity == Rule.Severity.Error);
        }

        public override void Clear()
        {
            base.Clear();

            m_NumCompilerErrors = 0;
        }

        protected override void OnDrawInfo()
        {
            EditorGUILayout.LabelField("- Use the Filters to reduce the number of reported issues");
            EditorGUILayout.LabelField("- Use the Mute button to mark an issue as false-positive");

            if (m_NumCompilerErrors > 0)
            {
                EditorGUILayout.Space();
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(Utility.ErrorIcon, GUILayout.MaxWidth(36));
                EditorGUILayout.LabelField(new GUIContent("Code Analysis is incomplete due to compilation errors"), GUILayout.Width(330), GUILayout.ExpandWidth(false));
                if (GUILayout.Button("View", EditorStyles.miniButton, GUILayout.Width(50)))
                    m_ViewManager.ChangeView(IssueCategory.CodeCompilerMessage);
                EditorGUILayout.EndHorizontal();
            }
        }
    }
}
