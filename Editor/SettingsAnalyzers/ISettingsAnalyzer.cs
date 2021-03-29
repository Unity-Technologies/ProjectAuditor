using System;
using System.Collections.Generic;

namespace Unity.ProjectAuditor.Editor.SettingsAnalyzers
{
    public interface ISettingsAnalyzer
    {
        void Initialize(IAuditor auditor);

        IEnumerable<ProjectIssue> Analyze();
    }
}
