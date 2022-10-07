using System.Collections.Generic;
using Unity.ProjectAuditor.Editor;
using Unity.ProjectAuditor.Editor.Core;
using UnityEditor;
using UnityEngine;

namespace Google.Android.OptimizationTool.Editor
{
    public interface ITextureAnalyzer
    {
        void Initialize(ProjectAuditorModule module);

        IEnumerable<ProjectIssue> Analyze(BuildTarget platform, Texture textureToAnalyze, TextureImporterPlatformSettings textureToAnalyzePlatformSettings);
    }
}
