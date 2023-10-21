using System.Collections.Generic;
using Unity.ProjectAuditor.Editor;
using Unity.ProjectAuditor.Editor.Core;
using UnityEditor;
using UnityEngine;

namespace Unity.ProjectAuditor.Editor.Interfaces
{
    internal interface IAudioClipModuleAnalyzer : IModuleAnalyzer
    {
        void PrepareForAnalysis(ProjectAuditorParams projectAuditorParams);
        
        IEnumerable<ProjectIssue> Analyze(ProjectAuditorParams projectAuditorParams, AudioImporter audioImporter);
    }
}
