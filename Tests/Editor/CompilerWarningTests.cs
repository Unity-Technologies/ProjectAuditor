using System;
using System.Linq;
using NUnit.Framework;
using Unity.ProjectAuditor.Editor;
using Unity.ProjectAuditor.Editor.AssemblyUtils;
using Unity.ProjectAuditor.Editor.Modules;
using Unity.ProjectAuditor.Editor.Utils;
using UnityEngine;
using UnityEngine.TestTools;

namespace Unity.ProjectAuditor.EditorTests
{
    class CompilerWarningTests : TestFixtureBase
    {
#pragma warning disable 0414
        TempAsset m_ScriptWithWarning;
#pragma warning restore 0414

        [OneTimeSetUp]
        public void SetUp()
        {
            m_ScriptWithWarning = new TempAsset("ScriptWithWarning.cs", @"
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

            // check descriptor
            Assert.IsNull(issue.descriptor);

            // check issue
            Assert.That(issue.category, Is.EqualTo(IssueCategory.CodeCompilerMessage));
            Assert.AreEqual("The variable 'i' is assigned but its value is never used", issue.description);
            Assert.True(issue.relativePath.StartsWith("Assets/"), "Relative path: " + issue.relativePath);
            Assert.That(issue.line, Is.EqualTo(5));
            Assert.That(issue.severity, Is.EqualTo(Rule.Severity.Warning));

            // check properties
            Assert.AreEqual((int)CompilerMessageProperty.Num, issue.GetNumCustomProperties());
            Assert.AreEqual("CS0219", issue.GetCustomProperty(CompilerMessageProperty.Code));
            Assert.AreEqual(AssemblyInfo.DefaultAssemblyName, issue.GetCustomProperty(CompilerMessageProperty.Assembly));
        }
    }
}
