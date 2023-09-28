using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using Unity.ProjectAuditor.Editor;
using Unity.ProjectAuditor.Editor.AssemblyUtils;
using Unity.ProjectAuditor.Editor.Core;
using Unity.ProjectAuditor.Editor.Diagnostic;
using Unity.ProjectAuditor.Editor.Modules;
using Unity.ProjectAuditor.Editor.Tests.Common;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace Unity.ProjectAuditor.EditorTests
{
    class UnexpectedCompilerMessageTests : TestFixtureBase
    {
#pragma warning disable 0414
        private TestAsset m_TestMcsRsp;
        TestAsset m_ScriptWithWarning;
        private TestAsset m_ScriptWithDiagnostic;
#pragma warning restore 0414

        private const string k_rspPath = "Assets/mcs.rsp";

        [OneTimeSetUp]
        public void SetUp()
        {
            // mcs.rsp can't be a TestAsset because it has to live directly in the root of Assets
            if (!File.Exists(k_rspPath))
                Directory.CreateDirectory(Path.GetDirectoryName(k_rspPath));

            File.WriteAllText(k_rspPath, "");
            Assert.True(File.Exists(k_rspPath));

            AssetDatabase.ImportAsset(k_rspPath, ImportAssetOptions.ForceUpdate);

            m_ScriptWithWarning = new TestAsset("ScriptWithWarning.cs", @"
class ScriptWithWarning {
    void SomeMethod()
    {
        int i = 0;
    }
}
");

            m_ScriptWithDiagnostic = new TestAsset("ScriptWithDiagnostic.cs", @"
using UnityEngine;
class MyClass
{
    void Dummy()
    {
        Debug.LogError(Camera.allCameras.Length.ToString());
    }
}
");
        }

        [OneTimeTearDown]
        public void TearDown()
        {
            if (File.Exists(k_rspPath))
            {
                AssetDatabase.DeleteAsset(k_rspPath);
                AssetDatabase.Refresh();
            }
        }

        [Test]
        public void UnexpectedCompilerMessage_DoesntStopCompilation()
        {
            using (var compilationHelper = new AssemblyCompilation())
            {
                var assemblyInfos = compilationHelper.Compile();

                Assert.Positive(assemblyInfos.Count());
            }
        }

        [Test]
        public void UnexpectedCompilerMessage_DoesntStopWarnings()
        {
            var issues = AnalyzeAndFindAssetIssues(m_ScriptWithWarning, IssueCategory.CodeCompilerMessage);

            Assert.AreEqual(1, issues.Count());

            var issue = issues.First();

            // check ID
            Assert.IsTrue(string.IsNullOrEmpty(issue.id));

            // check issue
            Assert.That(issue.category, Is.EqualTo(IssueCategory.CodeCompilerMessage));
            Assert.AreEqual("The variable 'i' is assigned but its value is never used", issue.description);
            Assert.True(issue.relativePath.StartsWith("Assets/"), "Relative path: " + issue.relativePath);
            Assert.That(issue.line, Is.EqualTo(5));
            Assert.That(issue.severity, Is.EqualTo(Severity.Warning));

            // check properties
            Assert.AreEqual((int)CompilerMessageProperty.Num, issue.GetNumCustomProperties());
            Assert.AreEqual("CS0219", issue.GetCustomProperty(CompilerMessageProperty.Code));
            Assert.AreEqual(AssemblyInfo.DefaultAssemblyName, issue.GetCustomProperty(CompilerMessageProperty.Assembly));
        }

        [Test]
        public void UnexpectedCompilerMessage_Issue_IsReported()
        {
            var issues = AnalyzeAndFindAssetIssues(m_ScriptWithDiagnostic);

            Assert.AreEqual(1, issues.Count());

            var myIssue = issues.FirstOrDefault();

            Assert.NotNull(myIssue);
            var descriptor = DescriptorLibrary.GetDescriptor(myIssue.id);
            Assert.NotNull(descriptor);

            Assert.AreEqual(Severity.Moderate, descriptor.defaultSeverity);
            Assert.AreEqual(typeof(string), myIssue.id.GetType());
            Assert.AreEqual("PAC0066", myIssue.id);
            Assert.AreEqual("UnityEngine.Camera", descriptor.type);
            Assert.AreEqual("allCameras", descriptor.method);

            Assert.AreEqual(m_ScriptWithDiagnostic.fileName, myIssue.filename);
            Assert.AreEqual("'UnityEngine.Camera.allCameras' usage", myIssue.description);
            Assert.AreEqual("System.Void MyClass::Dummy()", myIssue.GetContext());
            Assert.AreEqual(7, myIssue.line);
            Assert.AreEqual(IssueCategory.Code, myIssue.category);

            // check custom property
            Assert.AreEqual((int)CodeProperty.Num, myIssue.GetNumCustomProperties());
            Assert.AreEqual(AssemblyInfo.DefaultAssemblyName, myIssue.GetCustomProperty(CodeProperty.Assembly));
        }
    }
}
