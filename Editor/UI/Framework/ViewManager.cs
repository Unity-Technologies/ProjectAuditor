using System;
using System.Collections.Generic;
using System.Linq;
using Unity.ProjectAuditor.Editor;
using Unity.ProjectAuditor.Editor.UI.Framework;

namespace Editor.UI.Framework
{
    public class ViewManager
    {
        IssueCategory[] m_Categories;
        AnalysisView[] m_Views;
        int m_ActiveViewIndex;

        public ViewManager(IssueCategory[] categories)
        {
            m_Categories = categories;
        }

        public void Create(ProjectAuditor projectAuditor, Preferences preferences, IProjectIssueFilter filter)
        {
            var views = new List<AnalysisView>();
            foreach (var category in m_Categories)
            {
                var layout = projectAuditor.GetLayout(category);
                var desc = ViewDescriptor.GetAll().First(d => d.category == category);

                var view = desc.type != null ? (AnalysisView)Activator.CreateInstance(desc.type) : new AnalysisView();
                view.Create(desc, layout, projectAuditor.config, preferences, filter);
                views.Add(view);
            }

            m_Views = views.ToArray();

            var issues = new List<ProjectIssue>();
            var modules = m_Categories.Select(projectAuditor.GetAuditor).Distinct();
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

        public AnalysisView GetActiveView()
        {
            return m_Views[m_ActiveViewIndex];
        }
    }
}
