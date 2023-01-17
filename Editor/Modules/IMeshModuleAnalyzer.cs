using System.Collections.Generic;
using Unity.ProjectAuditor.Editor.Core;
using UnityEditor;

namespace Unity.ProjectAuditor.Editor.Modules
{
    public interface IMeshModuleAnalyzer : IModuleAnalyzer
    {
        IEnumerable<ProjectIssue> Analyze(ProjectAuditorParams projectAuditorParams, AssetImporter assetImporter);
    }
}
