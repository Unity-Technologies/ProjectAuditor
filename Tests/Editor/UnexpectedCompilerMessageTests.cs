using System.IO;
using System.Linq;
using NUnit.Framework;
using Unity.ProjectAuditor.Editor;
using Unity.ProjectAuditor.Editor.AssemblyUtils;
using Unity.ProjectAuditor.Editor.Modules;
using Unity.ProjectAuditor.Editor.Tests.Common;
using UnityEditor;

namespace Unity.ProjectAuditor.EditorTests
{
    class UnexpectedCompilerMessageTests : TestFixtureBase
    {
        TestAsset m_ScriptWithWarning;
        TestAsset m_ScriptWithDiagnostic;

        const string k_RspPath = "Assets/mcs.rsp";

        [OneTimeSetUp]
        public void SetUp()
        {
            // mcs.rsp can't be a TestAsset because it has to live directly in the root of Assets
            if (!File.Exists(k_RspPath))
                Directory.CreateDirectory(Path.GetDirectoryName(k_RspPath));

            File.WriteAllText(k_RspPath, "");
            Assert.True(File.Exists(k_RspPath));

            AssetDatabase.ImportAsset(k_RspPath, ImportAssetOptions.ForceUpdate);

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
            if (File.Exists(k_RspPath))
            {
                AssetDatabase.DeleteAsset(k_RspPath);
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

        [Test]
        public void UnexpectedCompilerMessage_Issue_IsReported()
        {
            var issues = AnalyzeAndFindAssetIssues(m_ScriptWithDiagnostic);

            Assert.AreEqual(1, issues.Count());

            var myIssue = issues.FirstOrDefault();

            Assert.NotNull(myIssue);
            var descriptor = myIssue.Id.GetDescriptor();
            Assert.NotNull(descriptor);

            Assert.AreEqual(Severity.Moderate, descriptor.DefaultSeverity);
            Assert.AreEqual(typeof(DescriptorId), myIssue.Id.GetType());
            Assert.AreEqual("PAC0066", myIssue.Id.ToString());
            Assert.AreEqual("UnityEngine.Camera", descriptor.Type);
            Assert.AreEqual("allCameras", descriptor.Method);

            Assert.AreEqual(m_ScriptWithDiagnostic.FileName, myIssue.Filename);
            Assert.AreEqual("'UnityEngine.Camera.allCameras' usage", myIssue.Description);
            Assert.AreEqual("System.Void MyClass::Dummy()", myIssue.GetContext());
            Assert.AreEqual(7, myIssue.Line);
            Assert.AreEqual(IssueCategory.Code, myIssue.Category);

            // check custom property
            Assert.AreEqual((int)CodeProperty.Num, myIssue.GetNumCustomProperties());
            Assert.AreEqual(AssemblyInfo.DefaultAssemblyName, myIssue.GetCustomProperty(CodeProperty.Assembly));
        }
    }
}
