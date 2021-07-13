using Editor.UI.Framework;
using Unity.ProjectAuditor.Editor.Auditors;
using Unity.ProjectAuditor.Editor.UI.Framework;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Unity.ProjectAuditor.Editor.UI
{
    [CustomEditor(typeof(BuildReport))]
    class BuildReportViewer : UnityEditor.Editor, IBuildReportProvider, IProjectIssueFilter
    {
        static int s_ActiveViewIndex;

        ViewManager m_ViewManager;
        Preferences m_Preferences;

        public override void OnInspectorGUI()
        {
            if (GetBuildReport() == null)
            {
                EditorGUILayout.HelpBox("No Build Report.", MessageType.Info);
                return;
            }

            if (m_ViewManager == null)
            {
                BuildReportModule.BuildReportProvider = this;

                var projectAuditor = new ProjectAuditor();
                m_Preferences = new Preferences();
                m_ViewManager = new ViewManager(new[] { IssueCategory.BuildStep, IssueCategory.BuildFile});
                m_ViewManager.Create(projectAuditor, m_Preferences, this);
                m_ViewManager.Audit(projectAuditor);
                m_ViewManager.activeViewIndex = s_ActiveViewIndex;
                m_ViewManager.onViewChanged = index => s_ActiveViewIndex = index;

                BuildReportModule.BuildReportProvider = null;
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

        public BuildReport GetBuildReport()
        {
            return target as BuildReport;
        }
    }
}
