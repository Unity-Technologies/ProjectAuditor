using System.Collections.Generic;
using Unity.ProjectAuditor.Editor.Core;
using UnityEditor;

namespace Unity.ProjectAuditor.Editor.Interfaces
{
    internal interface ISpriteAtlasModuleAnalyzer : IModuleAnalyzer
    {
        void PrepareForAnalysis(ProjectAuditorParams projectAuditorParams);
        
        IEnumerable<ProjectIssue> Analyze(ProjectAuditorParams projectAuditorParams,
            string assetPath);
    }
}
