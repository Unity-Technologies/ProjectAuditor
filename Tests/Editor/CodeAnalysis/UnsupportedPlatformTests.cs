using System.Linq;
using NUnit.Framework;
using Unity.ProjectAuditor.Editor.Core;
using Unity.ProjectAuditor.Editor.Diagnostic;
using Unity.ProjectAuditor.Editor.Tests.Common;
using UnityEditor;
using UnityEditor.TestTools;

namespace Unity.ProjectAuditor.EditorTests
{
    internal class UnsupportedPlatformTests : TestFixtureBase
    {
        TestAsset m_TestAssetMicrophone;

        [OneTimeSetUp]
        public void SetUp()
        {
            m_TestAssetMicrophone = new TestAsset("MicrophoneUsageTest.cs", @"
using UnityEngine;
class MicrophoneUsageTest
{
    string[] Test()
    {
        return Microphone.devices;
    }
}
");
        }

        [Test]
        [RequirePlatformSupport(BuildTarget.WebGL)]
        public void CodeAnalysis_PlatformIssue_IsReported()
        {
            m_Platform = BuildTarget.WebGL;

            var issue = AnalyzeAndFindAssetIssues(m_TestAssetMicrophone).FirstOrDefault(i => i.Id.Equals("PAC0233"));

            Assert.NotNull(issue);
            Assert.AreEqual("'UnityEngine.Microphone.get_devices' usage", issue.description);
            Assert.IsTrue(DescriptorLibrary.TryGetDescriptor(issue.Id, out var descriptor));
            Assert.Contains(Area.Support.ToString(), descriptor.areas);
        }

        [Test]
        public void CodeAnalysis_PlatformIssue_IsNotReported()
        {
            var diagnostic = AnalyzeAndFindAssetIssues(m_TestAssetMicrophone).FirstOrDefault(i => i.Id.Equals("PAC0233"));

            Assert.Null(diagnostic);
        }
    }
}
