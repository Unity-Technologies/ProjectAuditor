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
    public class AssemblyCompilationErrorTests
    {
#pragma warning disable 0414
        TempAsset m_TempAsset;
#pragma warning restore 0414

#if UNITY_EDITOR_WIN
        const string m_ExpectedMessage = "Assets\\ProjectAuditor-Temp\\MyClass.cs(6,1): error CS1519: Invalid token '}' in class, struct, or interface member declaration";
#else
        const string m_ExpectedMessage = "Assets/ProjectAuditor-Temp/MyClass.cs(6,1): error CS1519: Invalid token '}' in class, struct, or interface member declaration";
#endif
        const string m_ExpectedCode = "CS1519";
        const string m_ExpectedDescription = "Invalid token '}' in class, struct, or interface member declaration";

        [OneTimeSetUp]
        public void SetUp()
        {
            m_TempAsset = new TempAsset("MyClass.cs", @"
class MyClass {
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
        public void CompilerMessageIsReported()
        {
            LogAssert.ignoreFailingMessages = true;

            CompilerMessage[] defaultAssemblyCompilerMessages = null;
            using (var compilationHelper = new AssemblyCompilationPipeline
               {
                   AssemblyCompilationFinished = (assemblyName, messages) =>
                   {
                       if (assemblyName.Equals(AssemblyInfo.DefaultAssemblyName))
                       {
                           defaultAssemblyCompilerMessages = messages;
                       }
                   }
               })
            {
                compilationHelper.Compile();
            }

            LogAssert.Expect(LogType.Error, m_ExpectedMessage);
            LogAssert.Expect(LogType.Error, "Failed to compile player scripts");
            LogAssert.ignoreFailingMessages = false;

            Assert.NotNull(defaultAssemblyCompilerMessages);
            Assert.AreEqual(1, defaultAssemblyCompilerMessages.Length);
            Assert.True(defaultAssemblyCompilerMessages[0].message.Equals(m_ExpectedMessage));
        }

        [Test]
        [ExplicitAttribute]
        public void CompilerMessageIssueIsReported()
        {
            LogAssert.ignoreFailingMessages = true;

            var issues = Utility.AnalyzeAndFindAssetIssues(m_TempAsset, IssueCategory.CodeCompilerMessages);

            LogAssert.ignoreFailingMessages = false;

            Assert.AreEqual(1, issues.Count());

            var issue = issues.First();

            // check descriptor
            Assert.That(issue.descriptor.area, Is.EqualTo(Area.Info.ToString()));

            // check issue
            Assert.That(issue.category, Is.EqualTo(IssueCategory.CodeCompilerMessages));
            Assert.True(issue.description.Equals(m_ExpectedDescription));
            Assert.That(issue.line, Is.EqualTo(6));
            Assert.That(issue.severity, Is.EqualTo(Rule.Severity.Error));

            // check properties
            Assert.AreEqual((int)CompilerMessageProperty.Num, issue.GetNumCustomProperties());
            Assert.True(issue.GetCustomProperty((int)CompilerMessageProperty.Code).Equals(m_ExpectedCode));
            Assert.True(issue.GetCustomProperty((int)CompilerMessageProperty.Assembly).Equals(AssemblyInfo.DefaultAssemblyName));
        }
    }
}
