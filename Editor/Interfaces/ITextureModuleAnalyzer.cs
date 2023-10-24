using System.Collections.Generic;
using Unity.ProjectAuditor.Editor;
using Unity.ProjectAuditor.Editor.Core;
using UnityEditor;
using UnityEngine;

namespace Unity.ProjectAuditor.Editor.Interfaces
{
    internal class TextureAnalysisContext
    {
        public string Name;
        public Texture Texture;
        public TextureImporter Importer;
        public TextureImporterPlatformSettings ImporterPlatformSettings;
    }

    internal interface ITextureModuleAnalyzer : IModuleAnalyzer
    {
        void PrepareForAnalysis(ProjectAuditorParams projectAuditorParams);

        IEnumerable<ProjectIssue> Analyze(ProjectAuditorParams projectAuditorParams, TextureAnalysisContext context);
    }
}
