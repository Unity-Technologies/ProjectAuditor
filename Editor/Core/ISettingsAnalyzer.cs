using System;
using System.Collections.Generic;
using UnityEditor;

namespace Unity.ProjectAuditor.Editor.Core
{
    public class SettingsAnalyzerContext
    {
        public BuildTarget platform;
    }

    public interface ISettingsAnalyzer
    {
        void Initialize(ProjectAuditorModule module);

        IEnumerable<ProjectIssue> Analyze(SettingsAnalyzerContext context);
    }
}
