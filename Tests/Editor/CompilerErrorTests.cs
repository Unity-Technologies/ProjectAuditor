using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Unity.ProjectAuditor.Editor;
using Unity.ProjectAuditor.Editor.AssemblyUtils;
using Unity.ProjectAuditor.Editor.Diagnostic;
using Unity.ProjectAuditor.Editor.Modules;
using Unity.ProjectAuditor.Editor.Tests.Common;
using Unity.ProjectAuditor.Editor.Utils;
using UnityEngine;
using UnityEngine.TestTools;

namespace Unity.ProjectAuditor.EditorTests
{
    class CompilerErrorTests : TestFixtureBase
    {
#pragma warning disable 0414
        TestAsset m_ScriptWithError;
        TestAsset m_TestAsmdef;
#pragma warning restore 0414

        static readonly string k_ExpectedDescription = "Invalid token '}' in class, record, struct, or interface member declaration";
        static readonly string k_ExpectedMessage = $"{PathUtils.Combine(TestAsset.TempAssetsFolder,"ScriptWithError.cs")}(6,1): error CS1519: {k_ExpectedDescription}";

        const string k_ExpectedCode = "CS1519";

        const string k_TempAssemblyFileName = "Unity.ProjectAuditor.Temp.asmdef";
        static string k_TempAssemblyName
        {
            get { return Path.GetFileNameWithoutExtension(k_TempAssemblyFileName); }
        }

        [OneTimeSetUp]
        public void SetUp()
        {
            m_ScriptWithError = new TestAsset("ScriptWithError.cs", @"
class ScriptWithError {
#if !UNITY_EDITOR
    asd
#endif
}
");

            // this is required so we have an assembly which fails to compile. By doing so the default assembly won't be compiled as it's missing a dependency
            m_TestAsmdef = new TestAsset(k_TempAssemblyFileName, @"
{
    ""name"": ""Unity.ProjectAuditor.Temp"",
    ""rootNamespace"": """",
    ""references"": [
    ],
    ""includePlatforms"": [],
    ""excludePlatforms"": [],
    ""allowUnsafeCode"": false,
    ""overrideReferences"": true,
    ""precompiledReferences"": [],
    ""autoReferenced"": true,
    ""defineConstraints"": [],
    ""versionDefines"": [],
    ""noEngineReferences"": false
}");
        }

        [Test]
        [Explicit]
        public void CompilerError_IsReported()
        {
            LogAssert.ignoreFailingMessages = true;

            CompilerMessage[] compilerMessages = null;
            using (var compilationPipeline = new AssemblyCompilation
               {
                   OnAssemblyCompilationFinished = (compilationTask, messages) =>
                   {
                       if (compilationTask.AssemblyName.Equals(k_TempAssemblyName))
                       {
                           compilerMessages = messages;
                       }
                   }
               })
            {
                compilationPipeline.Compile();
            }

            //LogAssert.Expect(LogType.Error, k_ExpectedMessage);
            //LogAssert.Expect(LogType.Error, "Failed to compile player scripts");
            LogAssert.ignoreFailingMessages = false;

            Assert.NotNull(compilerMessages);
            Assert.AreEqual(1, compilerMessages.Length);
            Assert.AreEqual(k_ExpectedCode, compilerMessages[0].Code);
            Assert.AreEqual(k_ExpectedDescription, compilerMessages[0].Message);
            Assert.AreEqual(CompilerMessageType.Error, compilerMessages[0].Type);
        }

        [Test]
        [Explicit]
        public void CompilerError_Message_IsReported()
        {
            LogAssert.ignoreFailingMessages = true;

            var issues = AnalyzeAndFindAssetIssues(m_ScriptWithError, IssueCategory.CodeCompilerMessage);

            LogAssert.ignoreFailingMessages = false;

            Assert.AreEqual(1, issues.Count());

            var issue = issues.First();

            // check ID
            Assert.IsFalse(issue.Id.IsValid());

            // check issue
            Assert.That(issue.Category, Is.EqualTo(IssueCategory.CodeCompilerMessage));
            Assert.AreEqual(k_ExpectedDescription, issue.Description, "Description: " + issue.Description);
            Assert.That(issue.Line, Is.EqualTo(6));
            Assert.That(issue.Severity, Is.EqualTo(Severity.Error));

            // check properties
            Assert.AreEqual((int)CompilerMessageProperty.Num, issue.GetNumCustomProperties());
            Assert.AreEqual(k_ExpectedCode, issue.GetCustomProperty(CompilerMessageProperty.Code));
            Assert.AreEqual(k_TempAssemblyName, issue.GetCustomProperty(CompilerMessageProperty.Assembly));
        }

        [Test]
        [Explicit]
        public void CompilerError_Assembly_IsReported()
        {
            LogAssert.ignoreFailingMessages = true;

            var issues = Analyze(IssueCategory.Assembly, i => i.Severity == Severity.Error && i.RelativePath.Equals(m_TestAsmdef.relativePath));

            LogAssert.ignoreFailingMessages = false;

            Assert.AreEqual(1, issues.Length);

            var issue = issues.First();

            // check ID
            Assert.IsFalse(issue.Id.IsValid());

            // check issue
            Assert.That(issue.Category, Is.EqualTo(IssueCategory.Assembly));
            Assert.That(issue.Severity, Is.EqualTo(Severity.Error));
            Assert.That(issue.Filename, Is.EqualTo(m_TestAsmdef.fileName));
        }

        [Test]
        [Explicit]
        public void CompilerError_AssemblyDependency_IsReported()
        {
            LogAssert.ignoreFailingMessages = true;

            var issues = Analyze(IssueCategory.Assembly, i => i.Severity == Severity.Error && i.Description.Equals(k_TempAssemblyName));

            LogAssert.ignoreFailingMessages = false;

            Assert.AreEqual(1, issues.Length);

            var issue = issues.First();

            // check ID
            Assert.IsFalse(issue.Id.IsValid());

            // check issue
            Assert.That(issue.Category, Is.EqualTo(IssueCategory.Assembly));
            Assert.That(issue.Severity, Is.EqualTo(Severity.Error));
            Assert.AreEqual(k_TempAssemblyFileName, issue.Filename);
        }
    }
}
