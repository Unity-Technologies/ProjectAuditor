using System;
using System.Linq;
using NUnit.Framework;
using Unity.ProjectAuditor.Editor;
using Unity.ProjectAuditor.Editor.Auditors;
using Unity.ProjectAuditor.Editor.Utils;
using UnityEngine;
using UnityEngine.TestTools;

namespace UnityEditor.ProjectAuditor.EditorTests
{
    class CompilerWarningTests
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

        [OneTimeTearDown]
        public void TearDown()
        {
            TempAsset.Cleanup();
        }

        [Test]
        public void CompilerWarningIssueIsReported()
        {
            var issues = Utility.AnalyzeAndFindAssetIssues(m_ScriptWithWarning, IssueCategory.CodeCompilerMessage);

            Assert.AreEqual(1, issues.Count());

            var issue = issues.First();

            // check descriptor
            Assert.AreEqual(1, issue.descriptor.GetAreas().Length);
            Assert.Contains(Area.Info, issue.descriptor.GetAreas());

            // check issue
            Assert.That(issue.category, Is.EqualTo(IssueCategory.CodeCompilerMessage));
            Assert.True(issue.description.Equals("The variable 'i' is assigned but its value is never used"));
            Assert.That(issue.line, Is.EqualTo(5));
            Assert.That(issue.severity, Is.EqualTo(Rule.Severity.Warning));

            // check properties
            Assert.AreEqual((int)CompilerMessageProperty.Num, issue.GetNumCustomProperties());
            Assert.True(issue.GetCustomProperty(CompilerMessageProperty.Code).Equals("CS0219"));
            Assert.True(issue.GetCustomProperty(CompilerMessageProperty.Assembly).Equals(AssemblyInfo.DefaultAssemblyName));
        }
    }
}
