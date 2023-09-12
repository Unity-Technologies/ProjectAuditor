using System.IO;
using NUnit.Framework;
using Unity.ProjectAuditor.Editor.Utils;

namespace Unity.ProjectAuditor.EditorTests
{
    public class RoslynAnalyzerPipelineTests
    {
        string m_Path =
            PathUtils.Combine(ProjectAuditor.Editor.ProjectAuditor.s_PackagePath, "RoslynAnalyzers");

        [Test]
        public void RoslynAnalyzerPipeline_Folder_Exists()
        {
            Assert.True(Directory.Exists(m_Path));
        }

        [Test]
        public void RoslynAnalyzerPipeline_Analyzer_Exists()
        {
            Assert.True(File.Exists(PathUtils.Combine(m_Path, "Domain_Reload_Analyzer.dll")));
            Assert.True(File.Exists(PathUtils.Combine(m_Path, "Domain_Reload_Analyzer.dll.meta")));
        }
    }
}
