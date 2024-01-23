using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Unity.ProjectAuditor.Editor.Core
{
    // stephenm TODO: Document
    public class TextureAnalysisContext : AnalysisContext
    {
        public string Name;
        public Texture Texture;
        public TextureImporter Importer;
        public TextureImporterPlatformSettings ImporterPlatformSettings;
        public long Size;
    }

    // stephenm TODO: Document
    internal abstract class TextureModuleAnalyzer : ModuleAnalyzer
    {
        public abstract IEnumerable<ReportItem> Analyze(TextureAnalysisContext context);
    }
}
