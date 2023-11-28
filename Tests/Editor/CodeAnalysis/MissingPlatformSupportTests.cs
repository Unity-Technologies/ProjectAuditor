using System;
using System.Linq;
using NUnit.Framework;
using Unity.ProjectAuditor.Editor;
using Unity.ProjectAuditor.Editor.Diagnostic;
using Unity.ProjectAuditor.Editor.InstructionAnalyzers;
using Unity.ProjectAuditor.Editor.Tests.Common;
using UnityEditor;
using UnityEditor.TestTools;


namespace Unity.ProjectAuditor.EditorTests
{
    class MissingPlatformSupportTests : TestFixtureBase
    {
        TestAsset m_TestAssetSystemNet;
        TestAsset m_TestAssetSystemThreading;
        TestAsset m_TestAssetMicrophone;

        BuildTarget m_PrevPlatform;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            m_TestAssetSystemNet = new TestAsset("SystemNetUsageTest.cs", @"
class SystemNetUsageTest
{
    bool IsClientConnected(System.Net.Sockets.TcpClient client)
    {
        return client.Connected;
    }
}
");

            m_TestAssetSystemThreading = new TestAsset("SystemThreadingUsageTest.cs", @"
class SystemThreadingUsageTest
{
    void Test()
    {
        System.Threading.Thread.Sleep(1000);
    }
}
");

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

        [SetUp]
        public void SetUp()
        {
            m_PrevPlatform = m_Platform;
        }

        [TearDown]
        public void TearDown()
        {
            m_Platform = m_PrevPlatform;
        }

        [Test]
        [RequirePlatformSupport(BuildTarget.WebGL)]
        public void CodeAnalysis_MissingPlatformSupport_SystemNetIsReportedOnWebGL()
        {
            m_Platform = BuildTarget.WebGL;

            var diagnostic = AnalyzeAndFindAssetIssues(m_TestAssetSystemNet).FirstOrDefault(i => i.Id.Equals(UnsupportedOnWebGLAnalyzer.k_DescriptorSystemNet.Id));

            Assert.NotNull(diagnostic);
            Assert.AreEqual("'System.Boolean System.Net.Sockets.TcpClient::get_Connected()' usage", diagnostic.Description);
            Assert.AreEqual(Areas.Support, diagnostic.Id.GetDescriptor().Areas);
        }

        [Test]
        [RequirePlatformSupport(BuildTarget.WebGL)]
        public void CodeAnalysis_MissingPlatformSupport_SystemThreadingIsReportedOnWebGL()
        {
            m_Platform = BuildTarget.WebGL;

            var diagnostic = AnalyzeAndFindAssetIssues(m_TestAssetSystemThreading).FirstOrDefault(i => i.Id.Equals(UnsupportedOnWebGLAnalyzer.k_DescriptorSystemThreading.Id));

            Assert.NotNull(diagnostic);
            Assert.AreEqual("'System.Void System.Threading.Thread::Sleep(System.Int32)' usage", diagnostic.Description);
            Assert.AreEqual(Areas.Support, diagnostic.Id.GetDescriptor().Areas);
        }

        [Test]
        [RequirePlatformSupport(BuildTarget.WebGL)]
        public void CodeAnalysis_MissingPlatformSupport_MicrophoneIsReportedOnWebGL()
        {
            m_Platform = BuildTarget.WebGL;

            var diagnostic = AnalyzeAndFindAssetIssues(m_TestAssetMicrophone).FirstOrDefault(i => i.Id.Equals(UnsupportedOnWebGLAnalyzer.k_DescriptorMicrophone.Id));

            Assert.NotNull(diagnostic);
            Assert.AreEqual("'System.String[] UnityEngine.Microphone::get_devices()' usage", diagnostic.Description);
            Assert.AreEqual(Areas.Support, diagnostic.Id.GetDescriptor().Areas);
        }

        [Test]
        public void CodeAnalysis_MissingPlatformSupport_IssueIsNotReported()
        {
            var diagnostic = AnalyzeAndFindAssetIssues(m_TestAssetMicrophone).FirstOrDefault(i => i.Id.Equals("PAC0233"));

            Assert.Null(diagnostic);
        }
    }
}
