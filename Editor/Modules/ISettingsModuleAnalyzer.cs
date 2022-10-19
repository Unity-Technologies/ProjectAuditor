using System;
using System.Collections.Generic;
using Unity.ProjectAuditor.Editor.Core;
using UnityEditor;

namespace Unity.ProjectAuditor.Editor.Modules
{
    public class SettingsAnalyzerContext
    {
        public BuildTarget platform;
    }

    public interface ISettingsModuleAnalyzer
    {
        void Initialize(ProjectAuditorModule module);

        IEnumerable<ProjectIssue> Analyze(SettingsAnalyzerContext context);
    }
}
