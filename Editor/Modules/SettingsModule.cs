using System;
using System.Collections.Generic;
using Unity.ProjectAuditor.Editor.SettingsAnalyzers;
using Unity.ProjectAuditor.Editor.Utils;
using UnityEditor;
using UnityEngine;

namespace Unity.ProjectAuditor.Editor.Auditors
{
    class SettingsModule : IProjectAuditorModule
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

        List<ISettingsAnalyzer> m_Analyzers;
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

            foreach (var type in TypeCache.GetTypesDerivedFrom(typeof(ISettingsAnalyzer)))
                AddAnalyzer(Activator.CreateInstance(type) as ISettingsAnalyzer);
        }

        public bool IsSupported()
        {
            return true;
        }

        public void RegisterDescriptor(ProblemDescriptor descriptor)
        {
            m_ProblemDescriptors.Add(descriptor);
        }

        public void Audit(Action<ProjectIssue> onIssueFound, Action onComplete = null, IProgress progress = null)
        {
            if (progress != null)
                progress.Start("Analyzing Settings", "Analyzing project settings", m_Analyzers.Count);

            foreach (var analyzer in m_Analyzers)
            {
                if (progress != null)
                    progress.Advance();

                foreach (var issue in analyzer.Analyze())
                {
                    onIssueFound(issue);
                }
            }

            if (progress != null)
                progress.Clear();

            if (onComplete != null)
                onComplete();
        }

        void AddAnalyzer(ISettingsAnalyzer analyzer)
        {
            analyzer.Initialize(this);
            m_Analyzers.Add(analyzer);
        }
    }
}
