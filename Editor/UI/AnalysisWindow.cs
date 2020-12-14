using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Unity.ProjectAuditor.Editor.UI
{
    class AnalysisWindow : EditorWindow
    {
        AnalysisView m_AnalysisView;

        public static AnalysisWindow FindOpenWindow()
        {
            UnityEngine.Object[] windows = Resources.FindObjectsOfTypeAll(typeof(AnalysisWindow));
            if (windows != null && windows.Length > 0)
                return windows[0] as AnalysisWindow;

            return null;
        }

        AnalysisWindow()
        {
            m_AnalysisView = new AnalysisView();
        }

        public void CreateTable(AnalysisViewDescriptor desc, ProjectAuditorConfig config, Preferences prefs, IProjectIssueFilter filter)
        {
            m_AnalysisView.CreateTable(desc, config, prefs, filter);
        }

        public void AddIssues(IEnumerable<ProjectIssue> issues)
        {
            m_AnalysisView.AddIssues(issues);
        }

        public void Refresh()
        {
            m_AnalysisView.Refresh();
        }

        public void Clear()
        {
            m_AnalysisView.Clear();
        }

        public bool IsValid()
        {
            return m_AnalysisView.IsValid();
        }

        public void OnGUI()
        {
            if (!m_AnalysisView.IsValid())
            {
                Close();
                return;
            }

            m_AnalysisView.OnGUI();
        }
    }
}
