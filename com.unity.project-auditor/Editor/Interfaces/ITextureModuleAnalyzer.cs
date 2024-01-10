using System.Collections.Generic;
using Unity.ProjectAuditor.Editor.Core;
using UnityEditor;
using UnityEngine;

namespace Unity.ProjectAuditor.Editor.Interfaces
{
    internal class TextureAnalysisContext : AnalysisContext
    {
        public string Name;
        public Texture Texture;
        public TextureImporter Importer;
        public TextureImporterPlatformSettings ImporterPlatformSettings;
        public int TextureStreamingMipmapsSizeLimit;
        public int TextureSizeLimit;
        public int SpriteAtlasEmptySpaceLimit;
    }

    internal interface ITextureModuleAnalyzer : IModuleAnalyzer
    {
        IEnumerable<ProjectIssue> Analyze(TextureAnalysisContext context);
    }
}
