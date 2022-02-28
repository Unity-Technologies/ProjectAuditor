using System;
using System.Linq;
using NUnit.Framework;
using Unity.ProjectAuditor.Editor;
using Unity.ProjectAuditor.Editor.Auditors;
using Unity.ProjectAuditor.Editor.Utils;
using UnityEngine;
using UnityEngine.TestTools;

namespace Unity.ProjectAuditor.EditorTests
{
    class CompilerErrorTests
    {
#pragma warning disable 0414
        TempAsset m_ScriptWithError;
#pragma warning restore 0414

#if UNITY_EDITOR_WIN
        const string k_ExpectedMessage = "Assets\\ProjectAuditor-Temp\\ScriptWithError.cs(6,1): error CS1519: Invalid token '}' in class, struct, or interface member declaration";
#else
        const string k_ExpectedMessage = "Assets/ProjectAuditor-Temp/ScriptWithError.cs(6,1): error CS1519: Invalid token '}' in class, struct, or interface member declaration";
#endif
        const string k_ExpectedCode = "CS1519";
        const string k_ExpectedDescription = "Invalid token '}' in class, record, struct, or interface member declaration";

        [OneTimeSetUp]
        public void SetUp()
        {
            m_ScriptWithError = new TempAsset("ScriptWithError.cs", @"
class ScriptWithError {
#if !UNITY_EDITOR
    asd
#endif
}
");
        }

        [OneTimeTearDown]
        public void TearDown()
        {
            TempAsset.Cleanup();
        }

        [Test]
        [ExplicitAttribute]
        public void CompilerError_IsReported()
        {
            LogAssert.ignoreFailingMessages = true;

            CompilerMessage[] defaultAssemblyCompilerMessages = null;
            using (var compilationPipeline = new AssemblyCompilationPipeline
               {
                   AssemblyCompilationFinished = (assemblyInfo, messages) =>
                   {
                       if (assemblyInfo.name.Equals(AssemblyInfo.DefaultAssemblyName))
                       {
                           defaultAssemblyCompilerMessages = messages;
                       }
                   }
               })
            {
                compilationPipeline.Compile();
            }

            LogAssert.Expect(LogType.Error, k_ExpectedMessage);
            LogAssert.Expect(LogType.Error, "Failed to compile player scripts");
            LogAssert.ignoreFailingMessages = false;

            Assert.NotNull(defaultAssemblyCompilerMessages);
            Assert.AreEqual(1, defaultAssemblyCompilerMessages.Length);
            Assert.AreEqual(k_ExpectedCode, defaultAssemblyCompilerMessages[0].code);
            Assert.AreEqual(k_ExpectedDescription, defaultAssemblyCompilerMessages[0].message);
            Assert.AreEqual(CompilerMessageType.Error, defaultAssemblyCompilerMessages[0].type);
        }

        [Test]
        [ExplicitAttribute]
        public void CompilerError_Message_IsReported()
        {
            LogAssert.ignoreFailingMessages = true;

            var issues = Utility.AnalyzeAndFindAssetIssues(m_ScriptWithError, IssueCategory.CodeCompilerMessage);

            LogAssert.ignoreFailingMessages = false;

            Assert.AreEqual(1, issues.Count());

            var issue = issues.First();

            // check descriptor
            Assert.Contains(Area.Info, issue.descriptor.GetAreas());

            // check issue
            Assert.That(issue.category, Is.EqualTo(IssueCategory.CodeCompilerMessage));
            Assert.AreEqual(k_ExpectedDescription, issue.description, "Description: " + issue.description);
            Assert.That(issue.line, Is.EqualTo(6));
            Assert.That(issue.severity, Is.EqualTo(Rule.Severity.Error));

            // check properties
            Assert.AreEqual((int)CompilerMessageProperty.Num, issue.GetNumCustomProperties());
            Assert.AreEqual(k_ExpectedCode, issue.GetCustomProperty(CompilerMessageProperty.Code));
            Assert.AreEqual(AssemblyInfo.DefaultAssemblyName, issue.GetCustomProperty(CompilerMessageProperty.Assembly));
        }

        [Test]
        [ExplicitAttribute]
        public void CompilerError_Assembly_IsReported()
        {
            LogAssert.ignoreFailingMessages = true;

            var issues = Utility.Analyze(IssueCategory.Assembly, i => i.severity == Rule.Severity.Error);

            LogAssert.ignoreFailingMessages = false;

            Assert.AreEqual(1, issues.Count());

            var issue = issues.First();

            // check descriptor
            Assert.Contains(Area.Info, issue.descriptor.GetAreas());

            // check issue
            Assert.That(issue.category, Is.EqualTo(IssueCategory.Assembly));
            Assert.That(issue.severity, Is.EqualTo(Rule.Severity.Error));
        }
    }
}
