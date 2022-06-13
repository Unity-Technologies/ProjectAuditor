using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.ProjectAuditor.Editor.SettingsAnalyzers;
using Unity.ProjectAuditor.Editor.Utils;
using UnityEditor;
using UnityEngine;

namespace Unity.ProjectAuditor.Editor.Modules
{
    class SettingsModule : ProjectAuditorModule
    {
        static readonly IssueLayout k_IssueLayout = new IssueLayout
        {
            category = IssueCategory.ProjectSetting,
            properties = new[]
            {
                new PropertyDefinition { type = PropertyType.Description, name = "Issue", longName = "Issue description"},
                new PropertyDefinition { type = PropertyType.Area, name = "Area", longName = "The area the issue might have an impact on"}
            }
        };

        List<ISettingsAnalyzer> m_Analyzers;
        List<ProblemDescriptor> m_ProblemDescriptors;

        public override IEnumerable<ProblemDescriptor> GetDescriptors()
        {
            return m_ProblemDescriptors;
        }

        public override IEnumerable<IssueLayout> GetLayouts()
        {
            yield return k_IssueLayout;
        }

        public override void Initialize(ProjectAuditorConfig config)
        {
            m_Analyzers = new List<ISettingsAnalyzer>();
            m_ProblemDescriptors = new List<ProblemDescriptor>();

            foreach (var type in TypeCache.GetTypesDerivedFrom(typeof(ISettingsAnalyzer)))
                AddAnalyzer(Activator.CreateInstance(type) as ISettingsAnalyzer);
        }

        public override void RegisterDescriptor(ProblemDescriptor descriptor)
        {
            m_ProblemDescriptors.Add(descriptor);
        }

        public override Task<IReadOnlyCollection<ProjectIssue>> AuditAsync(ProjectAuditorParams projectAuditorParams, IProgress progress = null)
        {
            var issues = new List<ProjectIssue>();
            foreach (var analyzer in m_Analyzers)
            {
                foreach (var issue in analyzer.Analyze(projectAuditorParams.platform))
                {
                    issues.Add(issue);
                }
            }

            IReadOnlyCollection<ProjectIssue> collection = issues.AsReadOnly();
            return Task.FromResult(collection);
        }

        void AddAnalyzer(ISettingsAnalyzer analyzer)
        {
            analyzer.Initialize(this);
            m_Analyzers.Add(analyzer);
        }
    }
}
