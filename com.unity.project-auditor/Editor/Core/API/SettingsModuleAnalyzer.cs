using System.Collections.Generic;

namespace Unity.ProjectAuditor.Editor.Core
{
    /// <summary>
    /// A context object passed by SettingsModule to a SettingsModuleAnalyzer's Analyze() method.
    /// </summary>
    public class SettingsAnalysisContext : AnalysisContext
    {
    }

    /// <summary>
    /// Abstract base class for an Analyzer to be invoked by SettingsModule
    /// </summary>
    public abstract class SettingsModuleAnalyzer : ModuleAnalyzer
    {
        /// <summary>
        /// Implement this method to detect Issues, construct ReportItem objects to describe them, and return them.
        /// </summary>
        /// <param name="context">Context object containing information necessary to perform analysis</param>
        /// <returns>An enumerator for a collection of ReportItem objects</returns>
        public abstract IEnumerable<ReportItem> Analyze(SettingsAnalysisContext context);
    }
}
