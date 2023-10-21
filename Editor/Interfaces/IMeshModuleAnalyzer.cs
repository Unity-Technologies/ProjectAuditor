using System.Collections.Generic;
using Unity.ProjectAuditor.Editor.Core;
using UnityEditor;

namespace Unity.ProjectAuditor.Editor.Interfaces
{
    internal interface IMeshModuleAnalyzer : IModuleAnalyzer
    {
        void PrepareForAnalysis(ProjectAuditorParams projectAuditorParams);
        
        IEnumerable<ProjectIssue> Analyze(ProjectAuditorParams projectAuditorParams, AssetImporter assetImporter);
    }
}
