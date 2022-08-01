#if !BUILD_REPORT_INSPECTOR_INSTALLED

using System;
using Unity.ProjectAuditor.Editor.UI.Framework;
using Unity.ProjectAuditor.Editor.Modules;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Serialization;

namespace Unity.ProjectAuditor.Editor.UI
{
    [CustomEditor(typeof(BuildReport))]
    class BuildReportViewer : UnityEditor.Editor, IBuildReportProvider
    {
        static int s_ActiveViewIndex;
        static GlobalStates s_GlobalStates;
        static BuildReport s_BuildReport;

        [SerializeField] int m_ActiveViewIndex;
        [SerializeField] GlobalStates m_GlobalStates;

        ViewManager m_ViewManager;

        static readonly IssueCategory[] k_Categories = {IssueCategory.BuildStep, IssueCategory.BuildFile};

        void InitializeIfNeeded()
        {
            if (m_GlobalStates == null)
            {
                m_GlobalStates = new GlobalStates();
                m_ActiveViewIndex = 0;
            }

            if (m_ViewManager == null || !m_ViewManager.IsValid())
            {
                BuildReportModule.BuildReportProvider = this;

                var projectAuditor = new ProjectAuditor();

                m_ViewManager = new ViewManager(k_Categories);
                m_ViewManager.Create(projectAuditor, m_GlobalStates);

                m_ViewManager.onViewChanged = index => m_ActiveViewIndex = index;

                var report = projectAuditor.Audit(new ProjectAuditorParams
                {
                    categories = k_Categories
                });

                m_ViewManager.AddIssues(report.GetAllIssues());
                m_ViewManager.ChangeView(k_Categories[m_ActiveViewIndex]);

                BuildReportModule.BuildReportProvider = BuildReportModule.DefaultBuildReportProvider;
            }
        }

        void OnEnable()
        {
            // restore prefs/active view when switching between report assets
            if (s_GlobalStates != null)
            {
                m_GlobalStates = s_GlobalStates;
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
            m_ViewManager?.SaveSettings();

            s_GlobalStates = m_GlobalStates;
            s_ActiveViewIndex = m_ActiveViewIndex;
        }

        public override void OnInspectorGUI()
        {
            InitializeIfNeeded();

            EditorGUILayout.BeginVertical(GUILayout.Height(Screen.height));
            var view = m_ViewManager.GetActiveView();
            view.DrawTopPanel();

            EditorGUI.BeginChangeCheck();
            view.DrawSearch();
            if (EditorGUI.EndChangeCheck())
                view.MarkDirty();

            view.DrawContent();
            EditorGUILayout.EndVertical();
        }

        public BuildReport GetBuildReport()
        {
            return target as BuildReport;
        }
    }
}

#endif
