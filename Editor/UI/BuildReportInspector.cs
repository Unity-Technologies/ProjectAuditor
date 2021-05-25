using System.Collections.Generic;
using System.Linq;
using Editor.UI.Framework;
using Unity.ProjectAuditor.Editor.UI.Framework;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Unity.ProjectAuditor.Editor.UI
{
    [CustomEditor(typeof(BuildReport))]
    class BuildReportInspector : UnityEditor.Editor, IProjectIssueFilter
    {
        ViewManager m_ViewManager;
        Preferences m_Preferences;

        BuildReport report
        {
            get { return target as BuildReport; }
        }

        public override void OnInspectorGUI()
        {
            if (report == null)
            {
                EditorGUILayout.HelpBox("No Build Report.", MessageType.Info);
                return;
            }

            if (m_ViewManager == null)
            {
                var projectAuditor = new Unity.ProjectAuditor.Editor.ProjectAuditor();
                m_Preferences = new Preferences();
                m_ViewManager = new ViewManager(new[] { IssueCategory.BuildSteps, IssueCategory.BuildFiles});
                m_ViewManager.Create(projectAuditor, m_Preferences, this);
            }

            EditorGUILayout.BeginVertical(GUILayout.Height(Screen.height));
            var view = m_ViewManager.GetActiveView();
            view.DrawInfo();
            view.DrawContent();
            EditorGUILayout.EndVertical();
        }

        public bool Match(ProjectIssue issue)
        {
            return true; // there is no search field
        }
    }
}
