using System.Linq;
using NUnit.Framework;
using Unity.ProjectAuditor.Editor;
using Unity.ProjectAuditor.Editor.AssemblyUtils;
using Unity.ProjectAuditor.Editor.Modules;
using Unity.ProjectAuditor.Editor.Tests.Common;

namespace Unity.ProjectAuditor.EditorTests
{
    class CompilerWarningTests : TestFixtureBase
    {
#pragma warning disable 0414
        TestAsset m_ScriptWithWarning;
#pragma warning restore 0414

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            m_ScriptWithWarning = new TestAsset("ScriptWithWarning.cs", @"
class ScriptWithWarning {
    void SomeMethod()
    {
        int i = 0;
    }
}
");
        }

        [Test]
        public void CompilerWarning_Issue_IsReported()
        {
            var issues = AnalyzeAndFindAssetIssues(m_ScriptWithWarning, IssueCategory.CodeCompilerMessage);

            Assert.AreEqual(1, issues.Count());

            var issue = issues.First();

            // check ID
            Assert.IsFalse(issue.Id.IsValid());

            // check issue
            Assert.That(issue.Category, Is.EqualTo(IssueCategory.CodeCompilerMessage));
            Assert.AreEqual("The variable 'i' is assigned but its value is never used", issue.Description);
            Assert.True(issue.RelativePath.StartsWith("Assets/"), "Relative path: " + issue.RelativePath);
            Assert.That(issue.Line, Is.EqualTo(5));
            Assert.That(issue.Severity, Is.EqualTo(Severity.Warning));

            // check properties
            Assert.AreEqual((int)CompilerMessageProperty.Num, issue.GetNumCustomProperties());
            Assert.AreEqual("CS0219", issue.GetCustomProperty(CompilerMessageProperty.Code));
            Assert.AreEqual(AssemblyInfo.DefaultAssemblyName, issue.GetCustomProperty(CompilerMessageProperty.Assembly));
        }
    }
}
