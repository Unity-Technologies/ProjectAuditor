using System;
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
        static Preferences s_Preferences;

        [SerializeField] int m_ActiveViewIndex;
        [SerializeField] Preferences m_Preferences;

        ViewManager m_ViewManager;

        void InitializeIfNeeded()
        {
            if (s_Preferences == null)
            {
                // static and non-static Preferences/ActiveViewIndex need to stay in sync so that they persists when user switches between reports
                s_Preferences = m_Preferences = new Preferences();
                s_ActiveViewIndex = m_ActiveViewIndex = 0;
            }
            else if (m_Preferences == null)
            {
                m_Preferences = s_Preferences;
                m_ActiveViewIndex = s_ActiveViewIndex;
            }

            if (m_ViewManager == null)
            {
                var projectAuditor = new ProjectAuditor();
                m_ViewManager = new ViewManager(new[] { IssueCategory.BuildStep, IssueCategory.BuildFile});
                m_ViewManager.Create(projectAuditor, m_Preferences, this);
                m_ViewManager.Audit(projectAuditor);
                m_ViewManager.activeViewIndex = m_ActiveViewIndex;
                m_ViewManager.onViewChanged = index => s_ActiveViewIndex = m_ActiveViewIndex = index;
                (m_ViewManager.GetView(IssueCategory.BuildStep) as BuildReportView).buildReportProvider = this;
                (m_ViewManager.GetView(IssueCategory.BuildFile) as BuildReportView).buildReportProvider = this;
            }
        }

        void OnEnable()
        {
            InitializeIfNeeded();
        }

        public override void OnInspectorGUI()
        {
            if (GetBuildReport() == null)
            {
                EditorGUILayout.HelpBox("No Build Report.", MessageType.Info);
                return;
            }

            InitializeIfNeeded();

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
