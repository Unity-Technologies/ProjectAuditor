using System;
using System.Collections.Generic;

namespace Unity.ProjectAuditor.Editor.SettingsAnalyzers
{
    public interface ISettingsAnalyzer
    {
        void Initialize(IProjectAuditorModule module);

        IEnumerable<ProjectIssue> Analyze();
    }
}
