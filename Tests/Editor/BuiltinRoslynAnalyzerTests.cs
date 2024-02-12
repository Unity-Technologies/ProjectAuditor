using System.IO;
using NUnit.Framework;
using Unity.ProjectAuditor.Editor;
using Unity.ProjectAuditor.Editor.Tests.Common;
using Unity.ProjectAuditor.Editor.Utils;

namespace Unity.ProjectAuditor.EditorTests
{
    class BuiltinRoslynAnalyzerTests : TestFixtureBase
    {
        bool m_SavedUseRoslynAnalyzers;

        string m_Path =
            PathUtils.Combine(ProjectAuditorPackage.Path, "RoslynAnalyzers");

#pragma warning disable 0414
        TestAsset m_ScriptWithStaticMember;
#pragma warning restore 0414

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            m_ScriptWithStaticMember = new TestAsset("ScriptWithStaticMember.cs", @"
class ScriptWithStaticMember
{
#pragma warning disable 0414
    static int s_Member = 0;
#pragma warning restore 0414
}
");

            m_SavedUseRoslynAnalyzers = UserPreferences.UseRoslynAnalyzers;

            UserPreferences.UseRoslynAnalyzers = true;
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            UserPreferences.UseRoslynAnalyzers = m_SavedUseRoslynAnalyzers;
        }

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

        // TODO - If we add other Roslyn analyzers which pipe to different issue categories (CompilerMessage by default), test them here
        [Test]
        public void RoslynAnalyzerPipeline_Issue_IsFound()
        {
            var issues = AnalyzeAndFindAssetIssues(m_ScriptWithStaticMember, IssueCategory.DomainReload);

            Assert.AreEqual(1, issues.Length);

            Assert.AreEqual("UDR0001", issues[0].GetCustomProperty(0));
            Assert.AreEqual("No method with [RuntimeInitializeOnLoadMethod] attribute", issues[0].Description);
        }
    }
}
