using System.Collections.Generic;
using Unity.ProjectAuditor.Editor;
using Unity.ProjectAuditor.Editor.Core;
using UnityEditor;
using UnityEngine;

namespace Unity.ProjectAuditor.Editor.Interfaces
{
    internal interface ITextureModuleAnalyzer : IModuleAnalyzer
    {
        IEnumerable<ProjectIssue> Analyze(ProjectAuditorParams projectAuditorParams,
            TextureImporter textureImporter,
            TextureImporterPlatformSettings textureImporterPlatformSettings);
    }
}
