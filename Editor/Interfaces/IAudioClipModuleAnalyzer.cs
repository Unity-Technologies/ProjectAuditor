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
        public string PlatformString;
        public int StreamingClipThresholdBytes;
        public int LongDecompressedClipThresholdBytes;
        public int LongCompressedMobileClipThresholdBytes;
        public int LoadInBackGroundClipSizeThresholdBytes;
    }

    internal interface IAudioClipModuleAnalyzer : IModuleAnalyzer
    {
        IEnumerable<ProjectIssue> Analyze(AudioClipAnalysisContext context);
    }
}
