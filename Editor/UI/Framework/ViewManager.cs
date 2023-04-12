using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using Unity.ProjectAuditor.Editor.Core;
using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;

namespace Unity.ProjectAuditor.Editor.UI.Framework
{
    [Serializable]
    internal sealed class ViewManager
    {
        class NullFilter : IProjectIssueFilter
        {
            public bool Match(ProjectIssue issue)
            {
                return true;
            }
        }

        AnalysisView[] m_Views;

        [SerializeField] IssueCategory[] m_Categories;
        [SerializeField] int m_ActiveViewIndex;

        internal int activeViewIndex
        {
            get { return m_ActiveViewIndex; }
        }

        internal int numViews => m_Views != null ? m_Views.Length : 0;

        internal Action<IssueCategory> onAnalyze;
        internal Action onViewExported;
        internal Action<int> onViewChanged;

        internal ViewManager()
            : this(ViewDescriptor.GetAll().Select(d => d.category).ToArray())
        {
        }

        internal ViewManager(IssueCategory[] categories)
        {
            m_Categories = categories;
            m_ActiveViewIndex = 0;
        }

        internal bool IsValid()
        {
            return m_Views != null && m_Views.Length > 0;
        }

        internal void AddIssues(IReadOnlyCollection<ProjectIssue> issues)
        {
            Profiler.BeginSample("ViewManager.AddIssues");
            foreach (var view in m_Views)
            {
                view.AddIssues(issues);
            }
            Profiler.EndSample();
        }

        internal void Clear()
        {
            foreach (var view in m_Views)
            {
                if (view != null)
                    view.Clear();
            }
        }

        internal void Create(ProjectAuditor projectAuditor, ViewStates viewStates, Action<ViewDescriptor, bool> onCreateView = null, IProjectIssueFilter filter = null)
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
                    Debug.LogWarning("[Project Auditor] Descriptor for " + ProjectAuditor.GetCategoryName(category) + " was not registered.");
                    continue;
                }
                var layout = projectAuditor.GetLayout(category);
                var isSupported = layout != null;

                if (onCreateView != null)
                    onCreateView(desc, isSupported);

                if (!isSupported)
                {
                    Debug.LogWarning("[Project Auditor] Layout for category " + ProjectAuditor.GetCategoryName(category) + " was not found.");
                    continue;
                }

                var view = desc.type != null ? (AnalysisView)Activator.CreateInstance(desc.type, this) : new AnalysisView(this);
                view.Create(desc, layout, projectAuditor.config, viewStates, filter);
                view.OnEnable();
                views.Add(view);
            }

            m_Views = views.ToArray();
            Profiler.EndSample();
        }

        internal void ClearView(IssueCategory category)
        {
            var view = GetView(category);
            if (view != null)
            {
                view.Clear();
            }
        }

        internal AnalysisView GetActiveView()
        {
            return m_Views[m_ActiveViewIndex];
        }

        internal AnalysisView GetView(int index)
        {
            return m_Views[index];
        }

        internal bool HasView(IssueCategory category)
        {
            return GetView(category) != null;
        }

        internal AnalysisView GetView(IssueCategory category)
        {
            return m_Views.FirstOrDefault(v => v.desc.category == category);
        }

        internal void ChangeView(IssueCategory category)
        {
            var activeView = GetActiveView();
            if (activeView.desc.category == category)
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

                if (onViewChanged != null)
                    onViewChanged(m_ActiveViewIndex);
            }
        }

        /// <summary>
        /// Mark all views as dirty. Use this to reload their tables.
        /// </summary>
        internal void MarkViewsAsDirty()
        {
            foreach (var view in m_Views)
            {
                view.MarkDirty();
            }
        }

        internal void LoadSettings()
        {
            if (!IsValid())
                return;

            foreach (var view in m_Views)
            {
                view.LoadSettings();
            }
        }

        internal void SaveSettings()
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
