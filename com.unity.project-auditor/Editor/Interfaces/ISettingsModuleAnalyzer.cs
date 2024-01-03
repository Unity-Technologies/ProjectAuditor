using System;
using System.Collections.Generic;
using Unity.ProjectAuditor.Editor.Core;
using UnityEditor;

namespace Unity.ProjectAuditor.Editor.Interfaces
{
    internal class SettingsAnalysisContext : AnalysisContext
    {
    }

    internal interface ISettingsModuleAnalyzer : IModuleAnalyzer
    {
        IEnumerable<ProjectIssue> Analyze(SettingsAnalysisContext context);
    }
}
