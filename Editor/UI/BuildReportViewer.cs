#if !BUILD_REPORT_INSPECTOR_INSTALLED

using System;
using Unity.ProjectAuditor.Editor.UI.Framework;
using Unity.ProjectAuditor.Editor.Auditors;
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
        static BuildReport s_BuildReport;

        [SerializeField] int m_ActiveViewIndex;
        [SerializeField] Preferences m_Preferences;

        ViewManager m_ViewManager;

        void InitializeIfNeeded()
        {
            if (m_Preferences == null)
            {
                m_Preferences = new Preferences();
                m_ActiveViewIndex = 0;
            }

            if (m_ViewManager == null || !m_ViewManager.IsValid())
            {
                BuildReportModule.BuildReportProvider = this;
                var projectAuditor = new ProjectAuditor();
                m_ViewManager = new ViewManager(new[] { IssueCategory.BuildStep, IssueCategory.BuildFile});
                m_ViewManager.Create(projectAuditor, m_Preferences, this);
                m_ViewManager.Audit(projectAuditor);
                m_ViewManager.activeViewIndex = m_ActiveViewIndex;
                m_ViewManager.onViewChanged = index => m_ActiveViewIndex = index;
                BuildReportModule.BuildReportProvider = BuildReportModule.DefaultBuildReportProvider;
            }
        }

        void OnEnable()
        {
            // restore prefs/active view when switching between report assets
            if (s_Preferences != null)
            {
                m_Preferences = s_Preferences;
                m_ActiveViewIndex = s_ActiveViewIndex;
            }

            var buildReport = GetBuildReport();
            if (s_BuildReport != buildReport)
            {
                s_BuildReport = buildReport;
                m_ViewManager = null; // trigger new audit
            }

            InitializeIfNeeded();
        }

        void OnDisable()
        {
            s_Preferences = m_Preferences;
            s_ActiveViewIndex = m_ActiveViewIndex;
        }

        public override void OnInspectorGUI()
        {
            InitializeIfNeeded();

            EditorGUILayout.BeginVertical(GUILayout.Height(Screen.height));
            var view = m_ViewManager.GetActiveView();
            view.DrawInfo();

            EditorGUI.BeginChangeCheck();
            view.DrawTextSearch();
            if (EditorGUI.EndChangeCheck())
                view.Refresh();

            view.DrawContent();
            EditorGUILayout.EndVertical();
        }

        public bool Match(ProjectIssue issue)
        {
            return true;
        }

        public BuildReport GetBuildReport()
        {
            return target as BuildReport;
        }
    }
}

#endif
