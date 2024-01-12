using System.Collections.Generic;
using Unity.ProjectAuditor.Editor.Core;

namespace Unity.ProjectAuditor.Editor.Interfaces
{
    internal class SettingsAnalysisContext : AnalysisContext
    {
    }

    internal interface ISettingsModuleAnalyzer : IModuleAnalyzer
    {
        IEnumerable<ReportItem> Analyze(SettingsAnalysisContext context);
    }
}
