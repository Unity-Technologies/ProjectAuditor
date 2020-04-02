namespace Unity.ProjectAuditor.Editor.SettingsAnalyzers
{
    public interface ISettingsAnalyzer
    {
        int GetDescriptorId();

        ProjectIssue Analyze();
    }
}
