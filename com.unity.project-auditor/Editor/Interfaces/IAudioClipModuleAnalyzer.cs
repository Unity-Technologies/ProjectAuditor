using System.Collections.Generic;
using Unity.ProjectAuditor.Editor.Core;
using UnityEditor;

namespace Unity.ProjectAuditor.Editor.Interfaces
{
    internal class AudioClipAnalysisContext : AnalysisContext
    {
        public AudioImporter Importer;
    }

    internal interface IAudioClipModuleAnalyzer : IModuleAnalyzer
    {
        IEnumerable<ReportItem> Analyze(AudioClipAnalysisContext context);
    }
}
