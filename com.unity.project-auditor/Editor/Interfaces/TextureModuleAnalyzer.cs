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
    }

    internal abstract class TextureModuleAnalyzer : ModuleAnalyzer
    {
        public abstract IEnumerable<ReportItem> Analyze(TextureAnalysisContext context);
    }
}
