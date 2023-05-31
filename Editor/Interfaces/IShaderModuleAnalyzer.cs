using System.Collections.Generic;
using Unity.ProjectAuditor.Editor.Core;
using UnityEngine;

namespace Unity.ProjectAuditor.Editor.Interfaces
{
    internal interface IShaderModuleAnalyzer : IModuleAnalyzer
    {
        IEnumerable<ProjectIssue> Analyze(ProjectAuditorParams projectAuditorParams, Shader shader, string assetPath);
    }
}
