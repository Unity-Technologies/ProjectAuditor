using System;

namespace Unity.ProjectAuditor.Editor.SettingsAnalyzers
{
    public interface ISettingsAnalyzer
    {
        void Initialize(IAuditor auditor);

        int GetDescriptorId();

        ProjectIssue Analyze();
    }
}
