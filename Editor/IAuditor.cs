using System.Collections.Generic;

namespace Unity.ProjectAuditor.Editor
{
    public interface IAuditor
    {
        string GetUIName();
        IEnumerable<ProblemDescriptor> GetDescriptors();
        void LoadDatabase(string path);
        void RegisterDescriptor(ProblemDescriptor descriptor);
        void Audit( ProjectReport projectReport);
    }
}