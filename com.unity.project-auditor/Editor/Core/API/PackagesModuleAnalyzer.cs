using System.Collections.Generic;
using UnityEditor.PackageManager;

namespace Unity.ProjectAuditor.Editor.Core
{
    // stephenm TODO: Document
    public class PackageAnalysisContext : AnalysisContext
    {
        public PackageInfo PackageInfo;
    }

    // stephenm TODO: Document
    internal abstract class PackagesModuleAnalyzer : ModuleAnalyzer
    {
        public abstract IEnumerable<ReportItem> Analyze(PackageAnalysisContext context);
    }
}
