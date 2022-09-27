using System.Collections.Generic;
using UnityEditor;
namespace Unity.ProjectAuditor.Editor.Core
{
    public interface ITextureAnalyzer
    {
        void Initialize(ProjectAuditorModule module);
        IEnumerable<ProjectIssue> Analyze(BuildTarget platform, TextureImporter currentimporter, TextureImporterPlatformSettings tips);  // Unsure if TextureImporterSettings or EditorUserBuildSettings (EditorUserBuildSettings.androidBuildSubtarget) needed
    }
}
