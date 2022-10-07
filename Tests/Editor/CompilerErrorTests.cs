using System;
using System.IO;
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
    class CompilerErrorTests : TestFixtureBase
    {
#pragma warning disable 0414
        TempAsset m_ScriptWithError;
        TempAsset m_TempAsmdef;
#pragma warning restore 0414

#if UNITY_2020_1_OR_NEWER
        static readonly string k_ExpectedDescription = "Invalid token '}' in class, record, struct, or interface member declaration";
#else
        static readonly string k_ExpectedDescription = "Invalid token '}' in class, struct, or interface member declaration";
#endif

#if UNITY_EDITOR_WIN
        static readonly string k_ExpectedMessage = $"Assets\\ProjectAuditor-Temp\\ScriptWithError.cs(6,1): error CS1519: {k_ExpectedDescription}";
#else
        static readonly  string k_ExpectedMessage = $"Assets/ProjectAuditor-Temp/ScriptWithError.cs(6,1): error CS1519: {k_ExpectedDescription}";
#endif
        const string k_ExpectedCode = "CS1519";

        const string k_TempAssemblyFileName = "Unity.ProjectAuditor.Temp.asmdef";
        static string k_TempAssemblyName
        {
            get { return Path.GetFileNameWithoutExtension(k_TempAssemblyFileName); }
        }

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

            // this is required to that we have an assembly which fails to compile. By doing so the default assembly won't be compiled as it's missing a dependency
            m_TempAsmdef = new TempAsset(k_TempAssemblyFileName, @"
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
                   onAssemblyCompilationFinished = (compilationTask, messages) =>
                   {
                       if (compilationTask.assemblyName.Equals(k_TempAssemblyName))
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
            Assert.AreEqual(k_ExpectedCode, compilerMessages[0].code);
            Assert.AreEqual(k_ExpectedDescription, compilerMessages[0].message);
            Assert.AreEqual(CompilerMessageType.Error, compilerMessages[0].type);
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

            // check descriptor
            Assert.IsNull(issue.descriptor);

            // check issue
            Assert.That(issue.category, Is.EqualTo(IssueCategory.CodeCompilerMessage));
            Assert.AreEqual(k_ExpectedDescription, issue.description, "Description: " + issue.description);
            Assert.That(issue.line, Is.EqualTo(6));
            Assert.That(issue.severity, Is.EqualTo(Rule.Severity.Error));

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

            var issues = Analyze(IssueCategory.Assembly, i => i.severity == Rule.Severity.Error && i.relativePath.Equals(m_TempAsmdef.relativePath));

            LogAssert.ignoreFailingMessages = false;

            Assert.AreEqual(1, issues.Length);

            var issue = issues.First();

            // check descriptor
            Assert.IsNull(issue.descriptor);

            // check issue
            Assert.That(issue.category, Is.EqualTo(IssueCategory.Assembly));
            Assert.That(issue.severity, Is.EqualTo(Rule.Severity.Error));
            Assert.That(issue.filename, Is.EqualTo(m_TempAsmdef.fileName));
        }

        [Test]
        [Explicit]
        public void CompilerError_AssemblyDependency_IsReported()
        {
            LogAssert.ignoreFailingMessages = true;

            var issues = Analyze(IssueCategory.Assembly, i => i.severity == Rule.Severity.Error && i.description.Equals(k_TempAssemblyName));

            LogAssert.ignoreFailingMessages = false;

            Assert.AreEqual(1, issues.Length);

            var issue = issues.First();

            // check descriptor
            Assert.IsNull(issue.descriptor);

            // check issue
            Assert.That(issue.category, Is.EqualTo(IssueCategory.Assembly));
            Assert.That(issue.severity, Is.EqualTo(Rule.Severity.Error));
            Assert.AreEqual(k_TempAssemblyFileName, issue.filename);
        }
    }
}
