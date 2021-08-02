using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Profiling;

namespace Unity.ProjectAuditor.Editor.UI.Framework
{
    [Serializable]
    public class ViewManager
    {
        AnalysisView[] m_Views;

        [SerializeField] IssueCategory[] m_Categories;
        [SerializeField] int m_ActiveViewIndex;

        public int activeViewIndex
        {
            get { return m_ActiveViewIndex; }
            set { m_ActiveViewIndex = value;  }
        }

        public Action onViewExported;
        public Action<int> onViewChanged;

        public ViewManager(IssueCategory[] categories)
        {
            m_Categories = categories;
        }

        public bool IsValid()
        {
            return m_Views != null && m_Views.Length > 0;
        }

        public void AddIssues(ProjectIssue[] issues)
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

        public void Create(ProjectAuditor projectAuditor, Preferences preferences, IProjectIssueFilter filter, Action<ViewDescriptor, bool> onCreateView = null)
        {
            Profiler.BeginSample("ViewManager.Create");
            var views = new List<AnalysisView>();
            foreach (var category in m_Categories)
            {
                var desc = ViewDescriptor.GetAll().First(d => d.category == category);
                var layout = projectAuditor.GetLayout(category);
                var isSupported = layout != null;

                if (onCreateView != null)
                    onCreateView(desc, isSupported);

                if (!isSupported)
                {
                    Debug.Log("Project Auditor module " + category + " is not supported.");
                    continue;
                }

                var view = desc.type != null ? (AnalysisView)Activator.CreateInstance(desc.type, this) : new AnalysisView(this);
                view.Create(desc, layout, projectAuditor.config, preferences, filter);
                view.OnEnable();
                views.Add(view);
            }

            m_Views = views.ToArray();
            Profiler.EndSample();
        }

        public void Audit(ProjectAuditor projectAuditor)
        {
            var issues = new List<ProjectIssue>();
            var modules = m_Categories.Select(projectAuditor.GetModule).Distinct();
            foreach (var module in modules)
            {
                module.Audit(issue => { issues.Add(issue); });
            }

            foreach (var view in m_Views)
            {
                view.AddIssues(issues);
                view.Refresh();
            }
        }

        public void ClearView(IssueCategory category)
        {
            var view = GetView(category);
            if (view != null)
            {
                view.Clear();
                view.Refresh();
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

        public AnalysisView GetView(IssueCategory category)
        {
            return m_Views.FirstOrDefault(v => v.desc.category == category);
        }

        public void ChangeView(IssueCategory category)
        {
            var activeView = GetActiveView();
            if (activeView.desc.category == category)
                return;

            var viewIndex = Array.IndexOf(m_Views, GetView(category));
            ChangeView(viewIndex);
        }

        public void ChangeView(int index)
        {
            var changeViewRequired = (m_ActiveViewIndex != index);
            if (changeViewRequired)
            {
                m_ActiveViewIndex = index;
                var activeView = m_Views[m_ActiveViewIndex];

                activeView.Refresh();

                if (onViewChanged != null)
                    onViewChanged(m_ActiveViewIndex);
            }
        }

        public void SaveSettings()
        {
            foreach (var view in m_Views)
            {
                view.SaveSettings();
            }
        }
    }
}
