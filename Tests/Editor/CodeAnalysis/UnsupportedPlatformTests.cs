using System.Linq;
using NUnit.Framework;
using Unity.ProjectAuditor.Editor.Diagnostic;
using UnityEditor;
using UnityEditor.TestTools;

namespace Unity.ProjectAuditor.EditorTests
{
    public class UnsupportedPlatformTests : TestFixtureBase
    {
        TempAsset m_TempAssetMicrophone;

        [OneTimeSetUp]
        public void SetUp()
        {
            m_TempAssetMicrophone = new TempAsset("MicrophoneUsageTest.cs", @"
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

            var diagnostic = AnalyzeAndFindAssetIssues(m_TempAssetMicrophone).FirstOrDefault(i => i.descriptor.id.Equals("PAC0233"));

            Assert.NotNull(diagnostic);
            Assert.AreEqual("'UnityEngine.Microphone.get_devices' usage", diagnostic.description);
            Assert.Contains(Area.Support.ToString(), diagnostic.descriptor.areas);
        }

        [Test]
        public void CodeAnalysis_PlatformIssue_IsNotReported()
        {
            var diagnostic = AnalyzeAndFindAssetIssues(m_TempAssetMicrophone).FirstOrDefault(i => i.descriptor.id.Equals("PAC0233"));

            Assert.Null(diagnostic);
        }
    }
}
