using System.Linq;
using NUnit.Framework;
using Unity.ProjectAuditor.Editor;
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
        public void Setup()
        {
            m_PrevPlatform = m_Platform;
        }

        [TearDown]
        public void TearDown()
        {
            m_Platform = m_PrevPlatform;
        }

        [Test]
        public void CodeAnalysis_MissingPlatformSupport_IssueIsNotReported()
        {
            AnalyzeTestAssets();

            var systemNetDiagnostic = GetIssuesForAsset(m_TestAssetSystemNet).FirstOrDefault(i => i.Id.Equals("PAC1005"));
            Assert.Null(systemNetDiagnostic);

            var systemThreadingDiagnostic = GetIssuesForAsset(m_TestAssetSystemThreading).FirstOrDefault(i => i.Id.Equals("PAC1006"));
            Assert.Null(systemThreadingDiagnostic);

            var microphoneDiagnostic = GetIssuesForAsset(m_TestAssetMicrophone).FirstOrDefault(i => i.Id.Equals("PAC0233"));
            Assert.Null(microphoneDiagnostic);
        }

        [Test]
        [RequirePlatformSupport(BuildTarget.WebGL)]
        public void CodeAnalysis_MissingPlatformSupport_IssuesAreReported()
        {
            m_Platform = BuildTarget.WebGL;
            AnalyzeTestAssets();

            var systemNetDiagnostic = GetIssuesForAsset(m_TestAssetSystemNet).FirstOrDefault(i => i.Id.Equals(UnsupportedOnWebGLAnalyzer.k_DescriptorSystemNet.Id));
            Assert.NotNull(systemNetDiagnostic);

            var systemThreadingDiagnostic = GetIssuesForAsset(m_TestAssetSystemThreading).FirstOrDefault(i => i.Id.Equals(UnsupportedOnWebGLAnalyzer.k_DescriptorSystemThreading.Id));
            Assert.NotNull(systemThreadingDiagnostic);

            var microphoneDiagnostic = GetIssuesForAsset(m_TestAssetMicrophone).FirstOrDefault(i => i.Id.Equals(UnsupportedOnWebGLAnalyzer.k_DescriptorMicrophone.Id));
            Assert.NotNull(microphoneDiagnostic);

            Assert.AreEqual("'System.Boolean System.Net.Sockets.TcpClient::get_Connected()' usage", systemNetDiagnostic.Description);
            Assert.AreEqual(Areas.Support, systemNetDiagnostic.Id.GetDescriptor().Areas);

            Assert.AreEqual("'System.Void System.Threading.Thread::Sleep(System.Int32)' usage", systemThreadingDiagnostic.Description);
            Assert.AreEqual(Areas.Support, systemThreadingDiagnostic.Id.GetDescriptor().Areas);

            Assert.AreEqual("'System.String[] UnityEngine.Microphone::get_devices()' usage", microphoneDiagnostic.Description);
            Assert.AreEqual(Areas.Support, microphoneDiagnostic.Id.GetDescriptor().Areas);
        }
    }
}
