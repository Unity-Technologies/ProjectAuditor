using System;

namespace Unity.ProjectAuditor.Editor
{
    public interface IProgress
    {
        void Start(string title, string description, int total);
        void Advance(string description = "");
        void Clear();
    }
}
