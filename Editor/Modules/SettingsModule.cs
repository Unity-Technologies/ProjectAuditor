using System;
using System.Collections.Generic;
using System.Linq;
using Unity.ProjectAuditor.Editor.Core;
using Unity.ProjectAuditor.Editor.Diagnostic;
using Unity.ProjectAuditor.Editor.SettingsAnalysis;
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
                new PropertyDefinition { type = PropertyType.Area, name = "Area", longName = "The area the issue might have an impact on"},
                new PropertyDefinition { type = PropertyType.Path, name = "Settings", defaultGroup = true},
            }
        };

        List<ISettingsAnalyzer> m_Analyzers;
        HashSet<Descriptor> m_Descriptors;

        public override string name => "Settings";

        public override IReadOnlyCollection<Descriptor> supportedDescriptors => m_Descriptors;

        public override IReadOnlyCollection<IssueLayout> supportedLayouts => new IssueLayout[] {k_IssueLayout};

        public override void Initialize(ProjectAuditorConfig config)
        {
            m_Analyzers = new List<ISettingsAnalyzer>();
            m_Descriptors = new HashSet<Descriptor>();

            foreach (var type in TypeCache.GetTypesDerivedFrom(typeof(ISettingsAnalyzer)))
                AddAnalyzer(Activator.CreateInstance(type) as ISettingsAnalyzer);
        }

        public override void RegisterDescriptor(Descriptor descriptor)
        {
            if (!m_Descriptors.Add(descriptor))
                throw new Exception("Duplicate descriptor with id: " + descriptor.id);
        }

        public override void Audit(ProjectAuditorParams projectAuditorParams, IProgress progress = null)
        {
            if (progress != null)
                progress.Start("Analyzing Settings", "Analyzing project settings", m_Analyzers.Count);

            var context = new SettingsAnalyzerContext { platform = projectAuditorParams.platform};

            foreach (var analyzer in m_Analyzers)
            {
                if (progress != null)
                    progress.Advance();

                var issues = analyzer.Analyze(context).ToArray();
                if (issues.Any())
                    projectAuditorParams.onIncomingIssues(issues);
            }

            progress?.Clear();
            projectAuditorParams.onModuleCompleted?.Invoke();
        }

        void AddAnalyzer(ISettingsAnalyzer analyzer)
        {
            analyzer.Initialize(this);
            m_Analyzers.Add(analyzer);
        }
    }
}
