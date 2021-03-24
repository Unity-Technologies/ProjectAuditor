using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Unity.ProjectAuditor.Editor.SettingsAnalyzers;
using Unity.ProjectAuditor.Editor.Utils;
using UnityEditor.Macros;
using UnityEngine;
using TypeInfo = Unity.ProjectAuditor.Editor.Utils.TypeInfo;

namespace Unity.ProjectAuditor.Editor.Auditors
{
    class SettingsAuditor : IAuditor
    {
        static readonly IssueLayout k_IssueLayout = new IssueLayout
        {
            category = IssueCategory.ProjectSettings,
            properties = new[]
            {
                new PropertyDefinition { type = PropertyType.Description, name = "Issue", longName = "Issue description"},
                new PropertyDefinition { type = PropertyType.Area, name = "Area", longName = "The area the issue might have an impact on"}
            }
        };

        private List<ISettingsAnalyzer> m_Analyzers;
        List<ProblemDescriptor> m_ProblemDescriptors;

        public IEnumerable<ProblemDescriptor> GetDescriptors()
        {
            return m_ProblemDescriptors;
        }

        public IEnumerable<IssueLayout> GetLayouts()
        {
            yield return k_IssueLayout;
        }

        public void Initialize(ProjectAuditorConfig config)
        {
            m_Analyzers = new List<ISettingsAnalyzer>();
            m_ProblemDescriptors = new List<ProblemDescriptor>();

            foreach (var type in TypeInfo.GetAllTypesInheritedFromInterface<ISettingsAnalyzer>())
                AddAnalyzer(Activator.CreateInstance(type) as ISettingsAnalyzer);
        }

        public void RegisterDescriptor(ProblemDescriptor descriptor)
        {
            m_ProblemDescriptors.Add(descriptor);
        }

        public void Audit(Action<ProjectIssue> onIssueFound, Action onComplete, IProgressBar progressBar = null)
        {
            if (progressBar != null)
                progressBar.Initialize("Analyzing Settings", "Analyzing project settings", m_Analyzers.Count);

            foreach (var analyzer in m_Analyzers)
            {
                if (progressBar != null)
                    progressBar.AdvanceProgressBar();

                foreach (var issue in analyzer.Analyze())
                {
                    onIssueFound(issue);
                }
            }

            if (progressBar != null)
                progressBar.ClearProgressBar();

            onComplete();
        }

        void AddAnalyzer(ISettingsAnalyzer analyzer)
        {
            analyzer.Initialize(this);
            m_Analyzers.Add(analyzer);
        }
    }
}
