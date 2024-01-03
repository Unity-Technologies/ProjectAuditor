using System.Collections.Generic;
using Unity.ProjectAuditor.Editor.Core;
using UnityEngine;

namespace Unity.ProjectAuditor.Editor.Interfaces
{
    internal class ShaderAnalysisContext : AnalysisContext
    {
        public string AssetPath;
        public Shader Shader;
    }

    internal interface IShaderModuleAnalyzer : IModuleAnalyzer
    {
        IEnumerable<ProjectIssue> Analyze(ShaderAnalysisContext context);
    }
}
