#if UNITY_2020_1_OR_NEWER

using System.IO;
using NUnit.Framework;
using Unity.ProjectAuditor.Editor;
using Unity.ProjectAuditor.Editor.Tests.Common;
using Unity.ProjectAuditor.Editor.Utils;

namespace Unity.ProjectAuditor.EditorTests
{
    public class BuiltinRoslynAnalyzerTests : TestFixtureBase
    {
        bool m_SavedUseRoslynAnalyzers;

        string m_Path =
            PathUtils.Combine(ProjectAuditor.Editor.ProjectAuditor.s_PackagePath, "RoslynAnalyzers");

#pragma warning disable 0414
        TestAsset m_ScriptWithStaticMember;
#pragma warning restore 0414

        [OneTimeSetUp]
        public void FixtureSetUp()
        {
            base.FixtureSetUp();

            m_SavedUseRoslynAnalyzers = UserPreferences.useRoslynAnalyzers;

            UserPreferences.useRoslynAnalyzers = true;
        }

        [OneTimeTearDown]
        public void FixtureTearDown()
        {
            UserPreferences.useRoslynAnalyzers = m_SavedUseRoslynAnalyzers;

            base.FixtureTearDown();
        }

        [OneTimeSetUp]
        public void SetUp()
        {
            m_ScriptWithStaticMember = new TestAsset("ScriptWithStaticMember.cs", @"
class ScriptWithStaticMember
{
#pragma warning disable 0414
    static int s_Member = 0;
#pragma warning restore 0414
}
");
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

        [Test]
        public void RoslynAnalyzerPipeline_Issue_IsFound()
        {
            var issues = AnalyzeAndFindAssetIssues(m_ScriptWithStaticMember, IssueCategory.CodeCompilerMessage);

            Assert.AreEqual(1, issues.Length);

            Assert.AreEqual("UDR0001", issues[0].GetCustomProperty(0));
            Assert.AreEqual("No method with [RuntimeInitializeOnLoadMethod] attribute", issues[0].description);
        }
    }
}
#endif
