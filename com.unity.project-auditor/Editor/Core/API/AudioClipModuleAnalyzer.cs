using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Unity.ProjectAuditor.Editor.Core
{
    internal class AudioClipAnalysisContext : AnalysisContext
    {
        public string Name;
        public AudioClip AudioClip;
        public AudioImporter Importer;
        public AudioImporterSampleSettings SampleSettings;
        public long ImportedSize;
        public long RuntimeSize;
    }

    internal abstract class AudioClipModuleAnalyzer : ModuleAnalyzer
    {
        public abstract IEnumerable<ReportItem> Analyze(AudioClipAnalysisContext context);
    }
}
