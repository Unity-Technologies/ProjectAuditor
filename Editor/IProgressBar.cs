using System;

namespace Unity.ProjectAuditor.Editor
{
    public interface IProgressBar
    {
        void Initialize(string title, string description, int total);
        void AdvanceProgressBar(string description = "");
        void ClearProgressBar();
    }
}
