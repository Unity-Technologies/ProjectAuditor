using System;
using System.Collections.Generic;
using System.Linq;
using Unity.ProjectAuditor.Editor.Core;
using Unity.ProjectAuditor.Editor.Diagnostic;
using Unity.ProjectAuditor.Editor.Interfaces;
using Unity.ProjectAuditor.Editor.SettingsAnalysis;
using Unity.ProjectAuditor.Editor.Utils;
using UnityEditor;
using UnityEngine;

namespace Unity.ProjectAuditor.Editor.Modules
{
    class SettingsModule : ProjectAuditorModuleWithAnalyzers<ISettingsModuleAnalyzer>
    {
        static readonly IssueLayout k_IssueLayout = new IssueLayout
        {
            category = IssueCategory.ProjectSetting,
            properties = new[]
            {
                new PropertyDefinition { type = PropertyType.Description, name = "Issue", longName = "Issue description"},
                new PropertyDefinition { type = PropertyType.Severity, format = PropertyFormat.String, name = "Severity"},
                new PropertyDefinition { type = PropertyType.Area, name = "Area", longName = "The area the issue might have an impact on"},
                new PropertyDefinition { type = PropertyType.Filename, name = "System", defaultGroup = true},
                new PropertyDefinition { type = PropertyType.Platform, name = "Platform"}
            }
        };

        public override string name => "Settings";

        public override IReadOnlyCollection<IssueLayout> supportedLayouts => new IssueLayout[] {k_IssueLayout};

        public override void Audit(ProjectAuditorParams projectAuditorParams, IProgress progress = null)
        {
            var analyzers = GetPlatformAnalyzers(projectAuditorParams.Platform);
            if (progress != null)
                progress.Start("Analyzing Settings", "Analyzing project settings", analyzers.Length);


            foreach (var analyzer in analyzers)
            {
                if (progress != null)
                    progress.Advance();

                var context = new SettingsAnalysisContext
                {
                    Params = projectAuditorParams
                };

                var issues = analyzer.Analyze(context).ToArray();
                if (issues.Any())
                    projectAuditorParams.OnIncomingIssues(issues);
            }

            progress?.Clear();
            projectAuditorParams.OnModuleCompleted?.Invoke();
        }
    }
}
