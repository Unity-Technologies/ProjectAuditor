namespace Unity.ProjectAuditor.Editor
{
    public interface IAuditor
    {
        void LoadDatabase(string path);
        void Audit( ProjectReport projectReport);
    }
}