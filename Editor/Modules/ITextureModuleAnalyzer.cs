using System.Collections.Generic;
using Unity.ProjectAuditor.Editor;
using Unity.ProjectAuditor.Editor.Core;
using UnityEditor;
using UnityEngine;

namespace Unity.ProjectAuditor.Editor.Modules
{
    public interface ITextureModuleAnalyzer
    {
        void Initialize(ProjectAuditorModule module);

        IEnumerable<ProjectIssue> Analyze(BuildTarget platform, TextureImporter textureImporter, TextureImporterPlatformSettings textureImporterPlatformSettings);
    }
}
