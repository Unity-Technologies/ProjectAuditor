using System.Collections.Generic;
using Unity.ProjectAuditor.Editor;
using Unity.ProjectAuditor.Editor.Core;
using UnityEditor;
using UnityEngine;

namespace Unity.ProjectAuditor.Editor.Interfaces
{
    internal class AudioClipAnalysisContext : AnalysisContext
    {
        public AudioImporter Importer;
    }

    internal interface IAudioClipModuleAnalyzer : IModuleAnalyzer
    {
        IEnumerable<ProjectIssue> Analyze(AudioClipAnalysisContext context);
    }
}
