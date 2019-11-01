namespace Unity.ProjectAuditor.Editor
{
    public interface IAuditor
    {
        string GetUIName();
        void LoadDatabase(string path);
        void Audit( ProjectReport projectReport, ProjectAuditorConfig config);
    }
}