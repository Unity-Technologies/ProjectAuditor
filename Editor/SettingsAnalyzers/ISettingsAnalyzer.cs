using System;
using System.Collections.Generic;
using UnityEditor;

namespace Unity.ProjectAuditor.Editor.SettingsAnalyzers
{
    public interface ISettingsAnalyzer
    {
        void Initialize(ProjectAuditorModule module);

        IEnumerable<ProjectIssue> Analyze(BuildTarget platform);
    }
}
