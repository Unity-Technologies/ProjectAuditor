using System.Collections.Generic;
using Unity.ProjectAuditor.Editor.Core;
using UnityEngine;

namespace Unity.ProjectAuditor.Editor.Interfaces
{
    // stephenm TODO: Document
    public class ShaderAnalysisContext : AnalysisContext
    {
        public string AssetPath;
        public Shader Shader;
    }

    // stephenm TODO: Document
    internal abstract class ShaderModuleAnalyzer : ModuleAnalyzer
    {
        public abstract IEnumerable<ReportItem> Analyze(ShaderAnalysisContext context);
    }
}
