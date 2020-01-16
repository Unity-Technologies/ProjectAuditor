using System;
using System.Collections.Generic;

namespace Unity.ProjectAuditor.Editor
{
    public interface IAuditor
    {
        IEnumerable<ProblemDescriptor> GetDescriptors();

        void LoadDatabase(string path);

        IEnumerable<Type> GetAnalyzerTypes(System.Reflection.Assembly assembly);

        void RegisterDescriptor(ProblemDescriptor descriptor);
        void Audit( ProjectReport projectReport, IProgressBar progressBar = null);
    }
}