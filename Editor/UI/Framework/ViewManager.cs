using System;
using System.Collections.Generic;
using System.Linq;
using Unity.ProjectAuditor.Editor.Core;
using Unity.ProjectAuditor.Editor.Interfaces;
using UnityEngine;
using UnityEngine.Profiling;

namespace Unity.ProjectAuditor.Editor.UI.Framework
{
    [Serializable]
    internal sealed class ViewManager
    {
        class NullFilter : IIssueFilter
        {
            public bool Match(ProjectIssue issue)
            {
                return true;
            }
        }

        ProjectReport m_Report;
        AnalysisView[] m_Views;

        [SerializeField] IssueCategory[] m_Categories;
        [SerializeField] int m_ActiveViewIndex;

        public ProjectReport Report => m_Report;

        public int NumViews => m_Views != null ? m_Views.Length : 0;

        // user interactions
        public Action<int> OnActiveViewChanged;
        public Action<bool> OnMajorOrCriticalIssuesVisibilityChanged;
        public Action<bool> OnIgnoredIssuesVisibilityChanged;

        // events that trigger future operations
        public Action<IssueCategory> OnAnalysisRequested;
        public Action<ProjectIssue[]>  OnSelectedIssuesIgnoreRequested;
        public Action<ProjectIssue[]>  OnSelectedIssuesDisplayRequested;
        public Action<ProjectIssue[]>  OnSelectedIssuesQuickFixRequested;
        public Action<ProjectIssue[]>  OnSelectedIssuesDocumentationRequested;

        // events based on past operations
        public Action OnViewExportCompleted;

        public ViewManager()
            : this(ViewDescriptor.GetAll().Select(d => d.category).ToArray())
        {
        }

        public ViewManager(IssueCategory[] categories)
        {
            m_Categories = categories;
            m_ActiveViewIndex = 0;
        }

        public bool IsValid()
        {
            return m_Views != null && m_Views.Length > 0;
        }

        public void AddIssues(IReadOnlyCollection<ProjectIssue> issues)
        {
            Profiler.BeginSample("ViewManager.AddIssues");
            foreach (var view in m_Views)
            {
                view.AddIssues(issues);
            }
            Profiler.EndSample();
        }

        public void Clear()
        {
            foreach (var view in m_Views)
            {
                if (view != null)
                    view.Clear();
            }
        }

        public void Create(SeverityRules rules, ViewStates viewStates, Action<ViewDescriptor, bool> onCreateView = null, IIssueFilter filter = null)
        {
            if (filter == null)
                filter = new NullFilter();

            Profiler.BeginSample("ViewManager.Create");
            var views = new List<AnalysisView>();
            foreach (var category in m_Categories)
            {
                var desc = ViewDescriptor.GetAll().FirstOrDefault(d => d.category == category);
                if (desc == null)
                {
                    Debug.LogWarning($"[{ProjectAuditor.DisplayName}] Descriptor for " + ProjectAuditor.GetCategoryName(category) + " was not registered.");
                    continue;
                }
                var layout = IssueLayout.GetLayout(category);
                var isSupported = layout != null;

                if (onCreateView != null)
                    onCreateView(desc, isSupported);

                if (!isSupported)
                {
                    Debug.LogWarning($"[{ProjectAuditor.DisplayName}] Layout for category " + ProjectAuditor.GetCategoryName(category) + " was not found.");
                    continue;
                }

                var view = desc.type != null ? (AnalysisView)Activator.CreateInstance(desc.type, this) : new AnalysisView(this);
                view.Create(desc, layout, rules, viewStates, filter);
                view.OnEnable();
                views.Add(view);
            }

            m_Views = views.ToArray();
            Profiler.EndSample();
        }

        public void ClearView(IssueCategory category)
        {
            var view = GetView(category);
            if (view != null)
            {
                view.Clear();
            }
        }

        public AnalysisView GetActiveView()
        {
            return m_Views[m_ActiveViewIndex];
        }

        public AnalysisView GetView(int index)
        {
            return m_Views[index];
        }

        public bool HasView(IssueCategory category)
        {
            return GetView(category) != null;
        }

        public AnalysisView GetView(IssueCategory category)
        {
            return m_Views.FirstOrDefault(v => v.Desc.category == category);
        }

        public void ChangeView(IssueCategory category)
        {
            var activeView = GetActiveView();
            if (activeView.Desc.category == category)
            {
                return;
            }

            var newView = GetView(category);
            if (newView == null)
                return; // assume the view was not registered

            ChangeView(Array.IndexOf(m_Views, newView));
        }

        void ChangeView(int index)
        {
            var changeViewRequired = (m_ActiveViewIndex != index);
            if (changeViewRequired)
            {
                m_ActiveViewIndex = index;

                if (OnActiveViewChanged != null)
                    OnActiveViewChanged(m_ActiveViewIndex);
            }
        }

        /// <summary>
        /// Mark all views as dirty. Use this to reload their tables.
        /// </summary>
        public void MarkViewsAsDirty()
        {
            foreach (var view in m_Views)
            {
                view.MarkDirty();
            }
        }

        public void OnAnalysisCompleted(ProjectReport report)
        {
            m_Report = report;
        }

        public void OnAnalysisRestored(ProjectReport report)
        {
            AddIssues(report.GetAllIssues());
            m_Report = report;
        }

        public void LoadSettings()
        {
            if (!IsValid())
                return;

            foreach (var view in m_Views)
            {
                view.LoadSettings();
            }
        }

        public void SaveSettings()
        {
            if (!IsValid())
                return;

            foreach (var view in m_Views)
            {
                view.SaveSettings();
            }
        }
    }
}
