using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;

namespace Unity.ProjectAuditor.Editor.UI
{
    class AnalysisWindow : EditorWindow
    {
        AnalysisView m_AnalysisView;

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
            m_AnalysisView.OnGUI();
        }
    }
}
